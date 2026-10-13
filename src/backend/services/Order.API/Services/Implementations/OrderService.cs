using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Order.API.Data;
using Order.API.DTOs;
using Order.API.Entities;
using Order.API.Services.Interfaces;

namespace Order.API.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _dbContext;
    private readonly IProductPricingService _pricingService;
    private readonly IVnPayService _vnPayService;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        OrderDbContext dbContext,
        IProductPricingService pricingService,
        IVnPayService vnPayService,
        IMapper mapper,
        ILogger<OrderService> logger)
    {
        _dbContext = dbContext;
        _pricingService = pricingService;
        _vnPayService = vnPayService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<OrderDetailDto>> CreateOrderAsync(
        Guid currentUserId, string currentUserRole, CreateOrderRequest request, string? clientIp = null, CancellationToken cancellationToken = default)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return ApiResponse<OrderDetailDto>.Fail("Đơn hàng phải có ít nhất một sản phẩm.", 400);
        }

        var isStaffOrAdmin = currentUserRole.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                             currentUserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        // Nếu là Staff/Admin tạo đơn tại quầy, cho phép chỉ định CustomerId nếu khách có tài khoản
        Guid targetUserId = currentUserId;
        if (isStaffOrAdmin && request.CustomerId.HasValue && request.CustomerId.Value != Guid.Empty)
        {
            targetUserId = request.CustomerId.Value;
        }

        // Tạo mã đơn hàng duy nhất: ORD-yyyyMMdd-XXXXX
        var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomSuffix = Random.Shared.Next(1000, 9999);
        var orderCode = $"ORD-{todayStr}-{randomSuffix}";

        // Đảm bảo không trùng orderCode
        while (await _dbContext.Orders.AnyAsync(o => o.OrderCode == orderCode, cancellationToken))
        {
            randomSuffix = Random.Shared.Next(1000, 9999);
            orderCode = $"ORD-{todayStr}-{randomSuffix}";
        }

        var customerName = !string.IsNullOrWhiteSpace(request.CustomerName)
            ? request.CustomerName.Trim()
            : $"Khách vãng lai #{randomSuffix}";

        var newOrder = new Entities.Order
        {
            UserId = targetUserId,
            OrderCode = orderCode,
            Status = OrderStatus.PENDING,
            Note = request.Note,
            CustomerName = customerName,
            DiscountAmount = Math.Max(0, request.DiscountAmount),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        decimal totalAmount = 0;
        var orderItems = new List<OrderItem>();

        foreach (var item in request.Items)
        {
            // 1. Kiểm tra Price Tampering & Lấy giá chuẩn từ Product Service / DB
            var priceCheck = await _pricingService.VerifyAndCalculatePriceAsync(item, cancellationToken);
            if (!priceCheck.IsSuccess)
            {
                return ApiResponse<OrderDetailDto>.Fail(priceCheck.ErrorMessage ?? "Không thể xác thực giá sản phẩm.", 400);
            }

            var verifiedUnitPrice = priceCheck.VerifiedUnitPrice;
            var subtotal = verifiedUnitPrice * item.Quantity;
            totalAmount += subtotal;

            // Xây dựng tóm tắt lựa chọn (options summary)
            var summaryParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(item.Size))
            {
                summaryParts.Add($"Size: {item.Size}");
            }
            if (item.Toppings != null && item.Toppings.Any())
            {
                summaryParts.Add($"Topping: {string.Join(", ", item.Toppings)}");
            }
            if (!string.IsNullOrWhiteSpace(item.Note))
            {
                summaryParts.Add($"Ghi chú: {item.Note}");
            }

            var optionsSummary = summaryParts.Any()
                ? string.Join(" | ", summaryParts)
                : item.OptionsSummary;

            var orderItem = new OrderItem
            {
                OrderId = newOrder.Id,
                ProductId = item.ProductId,
                ProductName = priceCheck.ProductName,
                UnitPrice = verifiedUnitPrice,
                Quantity = item.Quantity,
                OptionsSummary = optionsSummary,
                Subtotal = subtotal
            };

            orderItems.Add(orderItem);
        }

        newOrder.TotalAmount = totalAmount;
        newOrder.FinalAmount = Math.Max(0, totalAmount - newOrder.DiscountAmount);
        newOrder.OrderItems = orderItems;

        // Tạo phương thức thanh toán ban đầu (hỗ trợ cả Tiếng Việt và Tiếng Anh)
        var initialPaymentMethod = PaymentMethod.Normalize(request.PaymentMethod);

        var isCashPaidAtPos = initialPaymentMethod == PaymentMethod.CASH && isStaffOrAdmin;

        var orderPayment = new OrderPayment
        {
            OrderId = newOrder.Id,
            PaymentMethod = initialPaymentMethod,
            Amount = newOrder.FinalAmount,
            PaymentStatus = isCashPaidAtPos ? PaymentStatus.SUCCESS : PaymentStatus.PENDING,
            PaidAt = isCashPaidAtPos ? DateTime.UtcNow : null,
            TransactionCode = isCashPaidAtPos ? $"CASH_POS_{randomSuffix}" : null,
            CreatedAt = DateTime.UtcNow
        };
        newOrder.OrderPayments.Add(orderPayment);

        if (isCashPaidAtPos)
        {
            newOrder.Status = OrderStatus.PROCESSING; // Thu tiền mặt tại quầy xong thì chuyển ngay sang pha chế
        }

        await _dbContext.Orders.AddAsync(newOrder, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tạo đơn hàng thành công: {OrderCode} (User: {UserId}, Tổng tiền: {FinalAmount}đ)",
            newOrder.OrderCode, targetUserId, newOrder.FinalAmount);

        // Ánh xạ bằng AutoMapper
        var orderDetailDto = _mapper.Map<OrderDetailDto>(newOrder);

        // Nếu phương thức thanh toán là VNPAY, tự động sinh luôn URL và mã QR để Staff/POS hiển thị ngay
        if (initialPaymentMethod == PaymentMethod.VNPAY)
        {
            try
            {
                var ip = !string.IsNullOrWhiteSpace(clientIp) ? clientIp : "127.0.0.1";
                orderDetailDto.VnPayPayment = _vnPayService.CreatePaymentUrl(newOrder, ip, request.ReturnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể tự động sinh URL VNPay cho đơn {OrderCode}", newOrder.OrderCode);
            }
        }

        return ApiResponse<OrderDetailDto>.Success(orderDetailDto, "Đặt hàng thành công.", 201);
    }

    public async Task<ApiResponse<PagedResult<OrderDto>>> GetOrdersAsync(
        Guid? currentUserId, string currentUserRole, OrderFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var isStaffOrAdmin = string.Equals(currentUserRole, "Staff", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(currentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);

        var query = _dbContext.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.OrderPayments)
            .AsNoTracking()
            .AsQueryable();

        // Customer chỉ xem đơn của chính mình (BOLA/IDOR protection)
        if (!isStaffOrAdmin)
        {
            if (!currentUserId.HasValue)
            {
                return ApiResponse<PagedResult<OrderDto>>.Fail("Không tìm thấy thông tin xác thực của người dùng.", 401);
            }
            query = query.Where(o => o.UserId == currentUserId.Value);
        }

        // Áp dụng bộ lọc
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var statusNormalized = OrderStatus.Normalize(filter.Status);
            query = query.Where(o => o.Status == statusNormalized);
        }

        if (!string.IsNullOrWhiteSpace(filter.OrderCode))
        {
            query = query.Where(o => o.OrderCode.Contains(filter.OrderCode.Trim()));
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= filter.ToDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = filter.PageNumber > 0 ? filter.PageNumber : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 10;

        var pagedOrders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Ánh xạ danh sách Order sang OrderDto bằng AutoMapper
        var items = _mapper.Map<List<OrderDto>>(pagedOrders);

        var pagedResult = new PagedResult<OrderDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<OrderDto>>.Success(pagedResult, "Lấy danh sách đơn hàng thành công.");
    }

    public async Task<ApiResponse<OrderDetailDto>> GetOrderDetailAsync(
        Guid orderId, Guid? currentUserId, string currentUserRole, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.OrderPayments)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            return ApiResponse<OrderDetailDto>.Fail("Không tìm thấy đơn hàng.", 404);
        }

        // Strict BOLA / IDOR Protection:
        // Nếu là Customer thì UserId bắt buộc phải trùng khớp với UserId của đơn
        var isStaffOrAdmin = string.Equals(currentUserRole, "Staff", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(currentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);

        if (!isStaffOrAdmin && currentUserId.HasValue && order.UserId != currentUserId.Value)
        {
            _logger.LogWarning("BOLA/IDOR Violation: User {UserId} cố gắng xem chi tiết đơn {OrderId} của User {OwnerId}",
                currentUserId, orderId, order.UserId);
            return ApiResponse<OrderDetailDto>.Fail("Bạn không có quyền truy cập đơn hàng này.", 403);
        }

        var detailDto = _mapper.Map<OrderDetailDto>(order);
        return ApiResponse<OrderDetailDto>.Success(detailDto, "Lấy chi tiết đơn hàng thành công.");
    }

    public async Task<ApiResponse<OrderDetailDto>> UpdateOrderStatusAsync(
        Guid orderId, UpdateOrderStatusRequest request, Guid currentUserId, string currentUserRole, CancellationToken cancellationToken = default)
    {
        var isStaffOrAdmin = string.Equals(currentUserRole, "Staff", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(currentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);

        if (!isStaffOrAdmin)
        {
            return ApiResponse<OrderDetailDto>.Fail("Chỉ nhân viên hoặc quản trị viên mới có quyền cập nhật trạng thái đơn hàng.", 403);
        }

        var nextStatus = OrderStatus.Normalize(request.Status);
        if (!OrderStatus.IsValid(nextStatus))
        {
            return ApiResponse<OrderDetailDto>.Fail($"Trạng thái '{request.Status}' không hợp lệ. Hỗ trợ: Tiếp nhận (PENDING), Pha chế (PROCESSING), Giao hàng (SHIPPING), Hoàn thành (COMPLETED), Hủy (CANCELLED).", 400);
        }

        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.OrderPayments)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            return ApiResponse<OrderDetailDto>.Fail("Không tìm thấy đơn hàng.", 404);
        }

        // Kiểm tra thứ tự chuyển trạng thái: Tiếp nhận -> Pha chế -> Giao hàng -> Hoàn thành
        if (!OrderStatus.CanTransition(order.Status, nextStatus))
        {
            return ApiResponse<OrderDetailDto>.Fail(
                $"Không thể chuyển trạng thái từ '{order.Status}' sang '{nextStatus}'. Quy trình bắt buộc: Tiếp nhận -> Pha chế -> Giao hàng -> Hoàn thành.", 400);
        }

        order.Status = nextStatus;
        order.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            order.Note = string.IsNullOrWhiteSpace(order.Note)
                ? request.Note
                : $"{order.Note} | {request.Note}";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cập nhật trạng thái đơn {OrderCode} -> {Status} bởi {UserId} ({Role})",
            order.OrderCode, nextStatus, currentUserId, currentUserRole);

        return ApiResponse<OrderDetailDto>.Success(_mapper.Map<OrderDetailDto>(order), $"Cập nhật trạng thái sang '{nextStatus}' thành công.");
    }

    public async Task<ApiResponse<OrderDetailDto>> CancelOrderAsync(
        Guid orderId, CancelOrderRequest request, Guid currentUserId, string currentUserRole, CancellationToken cancellationToken = default)
    {
        var isStaff = string.Equals(currentUserRole, "Staff", StringComparison.OrdinalIgnoreCase);

        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.OrderPayments)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            return ApiResponse<OrderDetailDto>.Fail("Không tìm thấy đơn hàng.", 404);
        }

        // BOLA / IDOR Protection: Nếu là Customer, chỉ được hủy đơn của chính mình
        if (!isStaff && order.UserId != currentUserId)
        {
            _logger.LogWarning("BOLA/IDOR Violation: User {UserId} cố gắng hủy đơn {OrderId} của User {OwnerId}",
                currentUserId, orderId, order.UserId);
            return ApiResponse<OrderDetailDto>.Fail("Bạn không có quyền hủy đơn hàng này.", 403);
        }

        // Đơn đã hoàn thành hoặc đã hủy trước đó
        if (order.Status == OrderStatus.COMPLETED)
        {
            return ApiResponse<OrderDetailDto>.Fail("Đơn hàng đã hoàn thành, không thể hủy.", 400);
        }

        if (order.Status == OrderStatus.CANCELLED)
        {
            return ApiResponse<OrderDetailDto>.Fail("Đơn hàng đã bị hủy trước đó.", 400);
        }

        // Điều kiện đối với Khách hàng: Chỉ được hủy khi đơn còn ở trạng thái "Tiếp nhận" (PENDING)
        if (!isStaff)
        {
            if (order.Status != OrderStatus.PENDING)
            {
                return ApiResponse<OrderDetailDto>.Fail(
                    $"Khách hàng chỉ có thể hủy đơn khi đang ở trạng thái 'Tiếp nhận'. Đơn hiện tại đang '{order.Status}', vui lòng liên hệ nhân viên quán để được hỗ trợ.", 400);
            }
        }
        else
        {
            // Điều kiện đối với Staff: Bắt buộc phải có lý do hủy
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return ApiResponse<OrderDetailDto>.Fail("Nhân viên (Staff) bắt buộc phải nhập lý do hủy đơn.", 400);
            }
        }

        order.Status = OrderStatus.CANCELLED;
        order.CancelReason = request.Reason.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Hủy đơn hàng {OrderCode} bởi {UserId} ({Role}). Lý do: {Reason}",
            order.OrderCode, currentUserId, currentUserRole, order.CancelReason);

        return ApiResponse<OrderDetailDto>.Success(_mapper.Map<OrderDetailDto>(order), "Hủy đơn hàng thành công.");
    }
}
