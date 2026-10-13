using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Order.API.Data;
using Order.API.DTOs;
using Order.API.Entities;
using Order.API.Services.Interfaces;

namespace Order.API.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly OrderDbContext _dbContext;
    private readonly IVnPayService _vnPayService;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        OrderDbContext dbContext,
        IVnPayService vnPayService,
        IMapper mapper,
        ILogger<PaymentService> logger)
    {
        _dbContext = dbContext;
        _vnPayService = vnPayService;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<ApiResponse<VnPayPaymentResponseDto>> GenerateVnPayQrAsync(
        Guid orderId, Guid? currentUserId, string currentUserRole, string clientIp, CancellationToken cancellationToken = default)
    {
        return GenerateVnPayPaymentUrlAsync(orderId, currentUserId, currentUserRole, clientIp, cancellationToken);
    }

    public async Task<ApiResponse<VnPayPaymentResponseDto>> GenerateVnPayPaymentUrlAsync(
        Guid orderId, Guid? currentUserId, string currentUserRole, string clientIp, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderPayments)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            return ApiResponse<VnPayPaymentResponseDto>.Fail("Không tìm thấy đơn hàng.", 404);
        }

        // Kiểm tra BOLA / IDOR: Khách hàng chỉ được tạo mã thanh toán cho đơn của chính mình
        var isStaffOrAdmin = string.Equals(currentUserRole, "Staff", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(currentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);

        if (!isStaffOrAdmin && currentUserId.HasValue && order.UserId != currentUserId.Value)
        {
            _logger.LogWarning("BOLA/IDOR Violation: User {UserId} cố gắng tạo mã thanh toán cho đơn {OrderId} của User {OwnerId}",
                currentUserId, orderId, order.UserId);
            return ApiResponse<VnPayPaymentResponseDto>.Fail("Bạn không có quyền truy cập đơn hàng này.", 403);
        }

        if (order.Status == OrderStatus.CANCELLED)
        {
            return ApiResponse<VnPayPaymentResponseDto>.Fail("Đơn hàng này đã bị hủy, không thể thanh toán.", 400);
        }

        var isAlreadyPaid = order.OrderPayments.Any(p => p.PaymentStatus == PaymentStatus.SUCCESS);
        if (isAlreadyPaid)
        {
            return ApiResponse<VnPayPaymentResponseDto>.Fail("Đơn hàng này đã được thanh toán thành công trước đó.", 400);
        }

        // Đảm bảo có record OrderPayment cho VNPay
        var vnpayPayment = order.OrderPayments.FirstOrDefault(p => p.PaymentMethod == PaymentMethod.VNPAY);
        if (vnpayPayment == null)
        {
            vnpayPayment = new OrderPayment
            {
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.VNPAY,
                Amount = order.FinalAmount,
                PaymentStatus = PaymentStatus.PENDING
            };
            await _dbContext.OrderPayments.AddAsync(vnpayPayment, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var paymentResponse = _vnPayService.CreatePaymentUrl(order, clientIp);
        return ApiResponse<VnPayPaymentResponseDto>.Success(paymentResponse, "Tạo đường dẫn thanh toán VNPay thành công.");
    }

    public async Task<ApiResponse<OrderPaymentDto>> ConfirmCashPaymentAsync(
        Guid orderId, Guid staffId, string staffRole, CashPaymentConfirmRequest? request, CancellationToken cancellationToken = default)
    {
        var isStaffOrAdmin = string.Equals(staffRole, "Staff", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(staffRole, "Admin", StringComparison.OrdinalIgnoreCase);

        if (!isStaffOrAdmin)
        {
            return ApiResponse<OrderPaymentDto>.Fail("Chỉ nhân viên hoặc quản trị viên mới có quyền xác nhận thu tiền mặt.", 403);
        }

        var order = await _dbContext.Orders
            .Include(o => o.OrderPayments)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            return ApiResponse<OrderPaymentDto>.Fail("Không tìm thấy đơn hàng.", 404);
        }

        if (order.Status == OrderStatus.CANCELLED)
        {
            return ApiResponse<OrderPaymentDto>.Fail("Đơn hàng đã bị hủy, không thể thu tiền mặt.", 400);
        }

        // Tìm hoặc tạo payment CASH
        var cashPayment = order.OrderPayments.FirstOrDefault(p => p.PaymentMethod == PaymentMethod.CASH);
        if (cashPayment == null)
        {
            cashPayment = new OrderPayment
            {
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.CASH,
                Amount = order.FinalAmount,
                PaymentStatus = PaymentStatus.PENDING
            };
            await _dbContext.OrderPayments.AddAsync(cashPayment, cancellationToken);
        }

        if (cashPayment.PaymentStatus == PaymentStatus.SUCCESS)
        {
            return ApiResponse<OrderPaymentDto>.Fail("Đơn hàng đã được xác nhận thanh toán tiền mặt trước đó.", 400);
        }

        cashPayment.PaymentStatus = PaymentStatus.SUCCESS;
        cashPayment.PaidAt = DateTime.UtcNow;
        cashPayment.TransactionCode = request?.TransactionCode ?? $"CASH_STAFF_{staffId.ToString()[..8]}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        if (order.Status == OrderStatus.PENDING)
        {
            order.Status = OrderStatus.PROCESSING; // Tiếp nhận xong chuyển sang pha chế khi đã thanh toán
            order.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Staff {StaffId} đã xác nhận thanh toán tiền mặt cho đơn {OrderCode}, số tiền {Amount}",
            staffId, order.OrderCode, order.FinalAmount);

        var paymentDto = _mapper.Map<OrderPaymentDto>(cashPayment);
        return ApiResponse<OrderPaymentDto>.Success(paymentDto, "Xác nhận thanh toán tiền mặt thành công.");
    }
}
