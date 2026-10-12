using Microsoft.EntityFrameworkCore;
using Order.API.Data;
using Order.API.DTOs;
using Order.API.Entities;
using Order.API.Hubs;
using Order.API.Services.Interfaces;

namespace Order.API.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _dbContext;
    private readonly IProductPricingService _pricingService;
    private readonly IOrderNotificationService _notificationService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        OrderDbContext dbContext,
        IProductPricingService pricingService,
        IOrderNotificationService notificationService,
        ILogger<OrderService> logger)
    {
        _dbContext = dbContext;
        _pricingService = pricingService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ApiResponse<OrderDetailDto>> CreateOrderAsync(
        Guid currentUserId, string currentUserRole, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return ApiResponse<OrderDetailDto>.Fail("Đơn hàng phải có ít nhất một sản phẩm.", 400);
        }

        var isStaffOrAdmin = string.Equals(currentUserRole, "Staff", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(currentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);

        // Nếu là Staff tạo đơn tại quầy, cho phép chỉ định CustomerId nếu khách có tài khoản
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

        var newOrder = new Entities.Order
        {
            UserId = targetUserId,
            OrderCode = orderCode,
            Status = OrderStatus.PENDING,
            Note = request.Note,
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

        var orderPayment = new OrderPayment
        {
            OrderId = newOrder.Id,
            PaymentMethod = initialPaymentMethod,
            Amount = newOrder.FinalAmount,
            PaymentStatus = PaymentStatus.PENDING,
            CreatedAt = DateTime.UtcNow
        };
        newOrder.OrderPayments.Add(orderPayment);

        await _dbContext.Orders.AddAsync(newOrder, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tạo đơn hàng thành công: {OrderCode} (User: {UserId}, Tổng tiền: {FinalAmount}đ)",
            newOrder.OrderCode, targetUserId, newOrder.FinalAmount);

        var orderDetailDto = MapToDetailDto(newOrder);

        // Bắn SignalR realtime cho Staff và Customer
        await _notificationService.NotifyOrderCreatedAsync(orderDetailDto);

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

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                OrderCode = o.OrderCode,
                TotalAmount = o.TotalAmount,
                DiscountAmount = o.DiscountAmount,
                FinalAmount = o.FinalAmount,
                Status = o.Status,
                CancelReason = o.CancelReason,
                Note = o.Note,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                TotalItems = o.OrderItems.Sum(i => i.Quantity),
                PaymentMethod = o.OrderPayments.OrderByDescending(p => p.CreatedAt).Select(p => p.PaymentMethod).FirstOrDefault(),
                PaymentStatus = o.OrderPayments.OrderByDescending(p => p.CreatedAt).Select(p => p.PaymentStatus).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

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

        var detailDto = MapToDetailDto(order);
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

        // Bắn SignalR realtime
        await _notificationService.NotifyOrderStatusChangedAsync(order.Id, order.OrderCode, order.UserId, nextStatus);

        return ApiResponse<OrderDetailDto>.Success(MapToDetailDto(order), $"Cập nhật trạng thái sang '{nextStatus}' thành công.");
    }

    public async Task<ApiResponse<OrderDetailDto>> CancelOrderAsync(
        Guid orderId, CancelOrderRequest request, Guid currentUserId, string currentUserRole, CancellationToken cancellationToken = default)
    {
        var isStaffOrAdmin = string.Equals(currentUserRole, "Staff", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(currentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);

        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.OrderPayments)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            return ApiResponse<OrderDetailDto>.Fail("Không tìm thấy đơn hàng.", 404);
        }

        // BOLA / IDOR Protection: Nếu là Customer, chỉ được hủy đơn của chính mình
        if (!isStaffOrAdmin && order.UserId != currentUserId)
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
        if (!isStaffOrAdmin)
        {
            if (order.Status != OrderStatus.PENDING)
            {
                return ApiResponse<OrderDetailDto>.Fail(
                    $"Khách hàng chỉ có thể hủy đơn khi đang ở trạng thái 'Tiếp nhận'. Đơn hiện tại đang '{order.Status}', vui lòng liên hệ nhân viên quán để được hỗ trợ.", 400);
            }
        }
        else
        {
            // Điều kiện đối với Staff / Admin: Bắt buộc phải có lý do hủy
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return ApiResponse<OrderDetailDto>.Fail("Nhân viên hoặc Quản trị viên bắt buộc phải nhập lý do hủy đơn.", 400);
            }
        }

        order.Status = OrderStatus.CANCELLED;
        order.CancelReason = request.Reason.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Hủy đơn hàng {OrderCode} bởi {UserId} ({Role}). Lý do: {Reason}",
            order.OrderCode, currentUserId, currentUserRole, order.CancelReason);

        // Bắn SignalR realtime
        await _notificationService.NotifyOrderStatusChangedAsync(order.Id, order.OrderCode, order.UserId, OrderStatus.CANCELLED, order.CancelReason);

        return ApiResponse<OrderDetailDto>.Success(MapToDetailDto(order), "Hủy đơn hàng thành công.");
    }

    private static OrderDetailDto MapToDetailDto(Entities.Order order)
    {
        var latestPayment = order.OrderPayments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();

        return new OrderDetailDto
        {
            Id = order.Id,
            UserId = order.UserId,
            OrderCode = order.OrderCode,
            TotalAmount = order.TotalAmount,
            DiscountAmount = order.DiscountAmount,
            FinalAmount = order.FinalAmount,
            Status = order.Status,
            CancelReason = order.CancelReason,
            Note = order.Note,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            TotalItems = order.OrderItems.Sum(i => i.Quantity),
            PaymentMethod = latestPayment?.PaymentMethod,
            PaymentStatus = latestPayment?.PaymentStatus,
            Items = order.OrderItems.Select(i => new OrderItemDto
            {
                Id = i.Id,
                OrderId = i.OrderId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                OptionsSummary = i.OptionsSummary,
                Subtotal = i.Subtotal
            }).ToList(),
            Payments = order.OrderPayments.Select(p => new OrderPaymentDto
            {
                Id = p.Id,
                OrderId = p.OrderId,
                PaymentMethod = p.PaymentMethod,
                TransactionCode = p.TransactionCode,
                Amount = p.Amount,
                PaymentStatus = p.PaymentStatus,
                PaidAt = p.PaidAt,
                CreatedAt = p.CreatedAt
            }).ToList()
        };
    }
}
