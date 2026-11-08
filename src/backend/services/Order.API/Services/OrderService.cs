using System.Text.Json;
using AutoMapper;
using Order.API.DTOs;
using Order.API.Entities;
using Order.API.Repositories;

namespace Order.API.Services;

/// <summary>
/// Dịch vụ xử lý toàn bộ logic nghiệp vụ Đơn hàng (Phần III - Order & Payment Service, mục 22 đến 27).
/// Tuân thủ kiến trúc phân tầng chuẩn: Controller -> Service -> Repository -> DbContext.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentService _paymentService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;
    private readonly INotificationClient _notificationClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OrderService(
        IOrderRepository orderRepository,
        IPaymentService paymentService,
        IHttpClientFactory httpClientFactory,
        IMapper mapper,
        ILogger<OrderService> logger,
         INotificationClient notificationClient)
    {
        _orderRepository = orderRepository;
        _paymentService = paymentService;
        _httpClientFactory = httpClientFactory;
        _mapper = mapper;
        _logger = logger;
        _notificationClient = notificationClient;
    }

    /// <summary>
    /// [Mục 22 - Create Order]: Tạo đơn hàng mới thông qua IOrderRepository.
    /// - Customer: Đặt hàng online (UserId lấy từ JWT token của khách).
    /// - Staff: Tạo đơn trực tiếp tại quầy POS (cho phép gắn CustomerId nếu khách có tài khoản thành viên).
    /// </summary>
    public async Task<ApiResponse<OrderDetailDto>> CreateOrderAsync(
        Guid currentUserId, string currentUserRole, CreateOrderRequest request, string? clientIp = null, CancellationToken cancellationToken = default)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return ApiResponse<OrderDetailDto>.Fail("Đơn hàng phải có ít nhất một sản phẩm.", 400);
        }

        var isStaff = currentUserRole.Equals("Staff", StringComparison.OrdinalIgnoreCase);

        // Nếu là Staff tạo đơn tại quầy POS, cho phép chỉ định CustomerId nếu khách đọc SĐT/tài khoản
        Guid targetUserId = currentUserId;
        if (isStaff && request.CustomerId.HasValue && request.CustomerId.Value != Guid.Empty)
        {
            targetUserId = request.CustomerId.Value;
        }

        // Tạo mã đơn hàng duy nhất theo định dạng: ORD-yyyyMMdd-XXXX
        var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomSuffix = Random.Shared.Next(1000, 9999);
        var orderCode = $"ORD-{todayStr}-{randomSuffix}";

        // Kiểm tra qua Repository để đảm bảo mã đơn hàng không bị trùng lặp trong Database
        while (await _orderRepository.ExistsByOrderCodeAsync(orderCode, cancellationToken))
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
            // Kiểm tra chống sửa giá từ Client (Anti Price-Tampering):
            // Luôn đối chiếu đơn giá chuẩn từ Product.API + phụ phí Size/Topping quy định ở Server
            var priceCheck = await VerifyAndCalculateProductPriceAsync(item, cancellationToken);
            if (!priceCheck.IsSuccess)
            {
                return ApiResponse<OrderDetailDto>.Fail(priceCheck.ErrorMessage ?? "Không thể xác thực giá sản phẩm.", 400);
            }

            var verifiedUnitPrice = priceCheck.VerifiedUnitPrice;
            var subtotal = verifiedUnitPrice * item.Quantity;
            totalAmount += subtotal;

            // Tổng hợp thông tin tùy chọn (Size, Topping, Ghi chú từng món)
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

        // Chuẩn hóa phương thức thanh toán (CASH hoặc VNPAY)
        var initialPaymentMethod = PaymentMethod.Normalize(request.PaymentMethod);

        // Nếu Staff tạo đơn tiền mặt (CASH) tại quầy POS: tự động xác nhận đã thu tiền và chuyển sang Pha chế (PROCESSING)
        var isCashPaidAtPos = initialPaymentMethod == PaymentMethod.CASH && isStaff;

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
            newOrder.Status = OrderStatus.PROCESSING;
        }

        // Lưu đơn hàng mới thông qua OrderRepository
        await _orderRepository.AddAsync(newOrder, cancellationToken);

        _logger.LogInformation("Tạo đơn hàng thành công: {OrderCode} (User: {UserId}, Tổng tiền: {FinalAmount}đ)",
            newOrder.OrderCode, targetUserId, newOrder.FinalAmount);

        var orderDetailDto = _mapper.Map<OrderDetailDto>(newOrder);

        // Nếu khách hoặc Staff chọn thanh toán VNPAY, tự động sinh URL & mã QR ngay trong kết quả trả về
        if (initialPaymentMethod == PaymentMethod.VNPAY)
        {
            try
            {
                var ip = !string.IsNullOrWhiteSpace(clientIp) ? clientIp : "127.0.0.1";
                orderDetailDto.VnPayPayment = _paymentService.CreatePaymentUrl(newOrder, ip, request.ReturnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể tự động sinh URL VNPay cho đơn {OrderCode}", newOrder.OrderCode);
            }
        }

        return ApiResponse<OrderDetailDto>.Success(orderDetailDto, "Đặt hàng thành công.", 201);
    }

    /// <summary>
    /// [Mục 23 & 25 - View Order List & Search/Filter Order]:
    /// Lấy danh sách đơn hàng có phân trang thông qua IOrderRepository.
    /// - Customer: Chỉ lấy các đơn hàng thuộc về chính Customer đó (BOLA Protection).
    /// - Staff: Lấy toàn bộ đơn hàng trên hệ thống để điều phối.
    /// </summary>
    public async Task<ApiResponse<PagedResult<OrderDto>>> GetOrdersAsync(
        Guid? currentUserId, string currentUserRole, OrderFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var isStaff = string.Equals(currentUserRole, "Staff", StringComparison.OrdinalIgnoreCase);

        // Bảo vệ BOLA/IDOR: Nếu không phải Staff, bắt buộc phải lọc theo UserId của chính khách hàng đó
        Guid? filterByUserId = null;
        if (!isStaff)
        {
            if (!currentUserId.HasValue)
            {
                return ApiResponse<PagedResult<OrderDto>>.Fail("Không tìm thấy thông tin xác thực của người dùng.", 401);
            }
            filterByUserId = currentUserId.Value;
        }

        var pageNumber = filter.PageNumber > 0 ? filter.PageNumber : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 10;

        // Gọi OrderRepository để truy vấn danh sách đơn hàng và tổng số bản ghi
        var (pagedOrders, totalCount) = await _orderRepository.GetFilteredAsync(
            filterByUserId, filter, cancellationToken);

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

    /// <summary>
    /// [Mục 24 - View Order Detail]: Xem thông tin chi tiết các món (items), tùy chọn và thanh toán trong đơn hàng.
    /// - Customer: Chỉ được xem đơn của chính mình.
    /// - Staff: Được xem chi tiết mọi đơn hàng.
    /// </summary>
    public async Task<ApiResponse<OrderDetailDto>> GetOrderDetailAsync(
        Guid orderId, Guid? currentUserId, string currentUserRole, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId, asNoTracking: true, cancellationToken);

        if (order == null)
        {
            return ApiResponse<OrderDetailDto>.Fail("Không tìm thấy đơn hàng.", 404);
        }

        // Kiểm tra BOLA / IDOR: Khách hàng không được xem đơn của người khác
        var isStaff = string.Equals(currentUserRole, "Staff", StringComparison.OrdinalIgnoreCase);

        if (!isStaff && currentUserId.HasValue && order.UserId != currentUserId.Value)
        {
            _logger.LogWarning("BOLA/IDOR Violation: User {UserId} cố gắng xem chi tiết đơn {OrderId} của User {OwnerId}",
                currentUserId, orderId, order.UserId);
            return ApiResponse<OrderDetailDto>.Fail("Bạn không có quyền truy cập đơn hàng này.", 403);
        }

        var detailDto = _mapper.Map<OrderDetailDto>(order);
        return ApiResponse<OrderDetailDto>.Success(detailDto, "Lấy chi tiết đơn hàng thành công.");
    }

    /// <summary>
    /// [Mục 26 - Update Order Status]: Cập nhật trạng thái đơn hàng theo đúng quy trình tuần tự:
    /// Pending (Tiếp nhận) -> Processing (Pha chế) -> Shipping (Giao hàng) -> Completed (Hoàn thành).
    /// Phân quyền: Chỉ Staff.
    /// </summary>
    public async Task<ApiResponse<OrderDetailDto>> UpdateOrderStatusAsync(
     Guid orderId,
     UpdateOrderStatusRequest request,
     Guid currentUserId,
     string currentUserRole,
     CancellationToken cancellationToken = default)
    {
        var isStaff = string.Equals(
            currentUserRole,
            "Staff",
            StringComparison.OrdinalIgnoreCase);

        if (!isStaff)
        {
            return ApiResponse<OrderDetailDto>.Fail(
                "Chỉ nhân viên (Staff) mới có quyền cập nhật trạng thái đơn hàng.",
                403);
        }

        var nextStatus = OrderStatus.Normalize(request.Status);

        if (!OrderStatus.IsValid(nextStatus))
        {
            return ApiResponse<OrderDetailDto>.Fail(
                $"Trạng thái '{request.Status}' không hợp lệ. Hỗ trợ: PENDING, PROCESSING, SHIPPING, COMPLETED, CANCELLED.",
                400);
        }

        var order = await _orderRepository.GetByIdWithDetailsAsync(
            orderId,
            asNoTracking: false,
            cancellationToken);

        if (order == null)
        {
            return ApiResponse<OrderDetailDto>.Fail(
                "Không tìm thấy đơn hàng.",
                404);
        }

        if (!OrderStatus.CanTransition(order.Status, nextStatus))
        {
            return ApiResponse<OrderDetailDto>.Fail(
                $"Không thể chuyển trạng thái từ '{order.Status}' sang '{nextStatus}'. Quy trình bắt buộc: Pending -> Processing -> Shipping -> Completed.",
                400);
        }

        // Nghiệp vụ: Phải thu tiền (trạng thái thanh toán SUCCESS) trước khi bấm Hoàn thành (COMPLETED)
        if (nextStatus == OrderStatus.COMPLETED)
        {
            var isPaid = order.OrderPayments.Any(p => p.PaymentStatus == PaymentStatus.SUCCESS);
            if (!isPaid)
            {
                return ApiResponse<OrderDetailDto>.Fail(
                    "Đơn hàng chưa được thanh toán. Vui lòng thu tiền (xác nhận thanh toán thành công) trước khi chuyển sang trạng thái Hoàn thành.",
                    400);
            }
        }

        // Cập nhật trạng thái đơn hàng
        order.Status = nextStatus;

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            order.Note = string.IsNullOrWhiteSpace(order.Note)
                ? request.Note
                : $"{order.Note} | {request.Note}";
        }

        // Lưu database trước khi gọi Notification API
        await _orderRepository.UpdateAsync(order, cancellationToken);

        _logger.LogInformation(
            "Cập nhật trạng thái đơn {OrderCode} -> {Status} bởi Staff {UserId}",
            order.OrderCode,
            nextStatus,
            currentUserId);

        // Gọi Notification API để thông báo cho khách hàng
        await _notificationClient.SendOrderNotificationAsync(
            new SendOrderNotificationRequest
            {
                UserId = order.UserId, // ID khách hàng sở hữu đơn hàng
                OrderId = order.Id,
                OrderStatus = nextStatus
            },
            cancellationToken);

        return ApiResponse<OrderDetailDto>.Success(
            _mapper.Map<OrderDetailDto>(order),
            $"Cập nhật trạng thái sang '{nextStatus}' thành công.");
    }

    /// <summary>
    /// [Mục 27 - Cancel Order]: Hủy đơn hàng thông qua IOrderRepository.
    /// - Customer: Chỉ được hủy đơn của chính mình khi đơn còn ở trạng thái mới tạo (PENDING).
    /// - Staff: Được phép hủy đơn (trừ đơn đã COMPLETED) và bắt buộc phải nhập lý do hủy.
    /// </summary>
    public async Task<ApiResponse<OrderDetailDto>> CancelOrderAsync(
        Guid orderId, CancelOrderRequest request, Guid currentUserId, string currentUserRole, CancellationToken cancellationToken = default)
    {
        var isStaff = string.Equals(currentUserRole, "Staff", StringComparison.OrdinalIgnoreCase);

        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId, asNoTracking: false, cancellationToken);

        if (order == null)
        {
            return ApiResponse<OrderDetailDto>.Fail("Không tìm thấy đơn hàng.", 404);
        }

        // Kiểm tra BOLA / IDOR: Nếu là Customer, chỉ được hủy đơn của chính mình
        if (!isStaff && order.UserId != currentUserId)
        {
            _logger.LogWarning("BOLA/IDOR Violation: User {UserId} cố gắng hủy đơn {OrderId} của User {OwnerId}",
                currentUserId, orderId, order.UserId);
            return ApiResponse<OrderDetailDto>.Fail("Bạn không có quyền hủy đơn hàng này.", 403);
        }

        if (order.Status == OrderStatus.COMPLETED)
        {
            return ApiResponse<OrderDetailDto>.Fail("Đơn hàng đã hoàn thành, không thể hủy.", 400);
        }

        if (order.Status == OrderStatus.CANCELLED)
        {
            return ApiResponse<OrderDetailDto>.Fail("Đơn hàng đã bị hủy trước đó.", 400);
        }

        // Quy tắc nghiệp vụ cho Customer: Chỉ được hủy khi đơn còn ở trạng thái PENDING (Tiếp nhận)
        if (!isStaff)
        {
            if (order.Status != OrderStatus.PENDING)
            {
                return ApiResponse<OrderDetailDto>.Fail(
                    $"Khách hàng chỉ có thể hủy đơn mới (trạng thái 'Pending'). Đơn hiện tại đang ở trạng thái '{order.Status}', vui lòng liên hệ nhân viên để được hỗ trợ.", 400);
            }
        }
        else
        {
            // Quy tắc nghiệp vụ cho Staff: Bắt buộc phải nhập lý do hủy đơn
            if (string.IsNullOrWhiteSpace(request?.Reason))
            {
                return ApiResponse<OrderDetailDto>.Fail("Nhân viên (Staff) bắt buộc phải nhập lý do khi hủy đơn hàng.", 400);
            }
        }

        order.Status = OrderStatus.CANCELLED;
        order.CancelReason = string.IsNullOrWhiteSpace(request?.Reason)
            ? "Khách hàng yêu cầu hủy đơn mới"
            : request.Reason.Trim();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        _logger.LogInformation("Hủy đơn hàng {OrderCode} bởi {UserId} ({Role}). Lý do: {Reason}",
            order.OrderCode, currentUserId, currentUserRole, order.CancelReason);

        return ApiResponse<OrderDetailDto>.Success(_mapper.Map<OrderDetailDto>(order), "Hủy đơn hàng thành công.");
    }

    /// <summary>
    /// Hàm nội bộ xác thực đơn giá sản phẩm từ Product.API để chống sửa giá từ phía Client (Anti Price-Tampering).
    /// </summary>
    private async Task<(bool IsSuccess, decimal VerifiedUnitPrice, string ProductName, string? ErrorMessage)> VerifyAndCalculateProductPriceAsync(
        CreateOrderItemRequest item,
        CancellationToken cancellationToken)
    {
        decimal sizeSurcharge = 0m;
        if (!string.IsNullOrWhiteSpace(item.Size))
        {
            var normalizedSize = item.Size.Trim().ToUpperInvariant();
            if (normalizedSize is "M" or "SIZE M" or "VỪA") sizeSurcharge = 6000m;
            else if (normalizedSize is "L" or "SIZE L" or "LỚN") sizeSurcharge = 10000m;
        }

        decimal toppingSurcharge = (item.Toppings?.Count ?? 0) * 10000m;

        try
        {
            var client = _httpClientFactory.CreateClient("ProductApi");
            var response = await client.GetAsync($"/api/products/{item.ProductId}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var dataElement = root.TryGetProperty("data", out var d) ? d : root;

                if (dataElement.TryGetProperty("isAvailable", out var isAvailProp) && !isAvailProp.GetBoolean())
                {
                    return (false, 0, string.Empty, $"Sản phẩm '{item.ProductName}' hiện đã hết hàng.");
                }

                decimal basePrice = 0;
                if (dataElement.TryGetProperty("basePrice", out var bp)) basePrice = bp.GetDecimal();
                else if (dataElement.TryGetProperty("price", out var p)) basePrice = p.GetDecimal();

                string productName = item.ProductName ?? "Sản phẩm LowLand Coffee";
                if (dataElement.TryGetProperty("name", out var nameProp) && !string.IsNullOrWhiteSpace(nameProp.GetString()))
                {
                    productName = nameProp.GetString()!;
                }

                if (basePrice > 0)
                {
                    var expectedTotalUnitPrice = basePrice + sizeSurcharge + toppingSurcharge;
                    var finalUnitPrice = item.UnitPrice >= basePrice
                        ? item.UnitPrice
                        : expectedTotalUnitPrice;

                    return (true, finalUnitPrice, productName, null);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể kết nối tới Product.API để kiểm tra sản phẩm {ProductId}. Sử dụng kiểm tra dự phòng.", item.ProductId);
        }

        if (item.UnitPrice < 10000m)
        {
            return (false, 0, string.Empty, "Đơn giá sản phẩm không hợp lệ (tối thiểu 10.000đ).");
        }

        var fallbackName = !string.IsNullOrWhiteSpace(item.ProductName)
            ? item.ProductName.Trim()
            : $"Sản phẩm #{item.ProductId.ToString()[..8]}";

        return (true, item.UnitPrice + sizeSurcharge + toppingSurcharge, fallbackName, null);
    }
}

