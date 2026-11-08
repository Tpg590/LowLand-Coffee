using System.Net;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Order.API.DTOs;
using Order.API.Entities;
using Order.API.Repositories;

namespace Order.API.Services;

/// <summary>
/// Dịch vụ xử lý Thanh toán đơn hàng & tích hợp cổng VNPay (Mục 28: Tạo QR VNPay, Mục 29: Xác nhận thanh toán).
/// Tuân thủ kiến trúc phân tầng chuẩn: Controller -> Service -> Repository -> DbContext.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        IConfiguration configuration,
        IMapper mapper,
        ILogger<PaymentService> logger)
    {
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _configuration = configuration;
        _mapper = mapper;
        _logger = logger;
    }

    private string TmnCode => !string.IsNullOrWhiteSpace(_configuration["VnPay:TmnCode"])
        ? _configuration["VnPay:TmnCode"]!
        : (Environment.GetEnvironmentVariable("VNPAY_TMN_CODE") ?? "9UCC6QWQ");

    private string HashSecret => !string.IsNullOrWhiteSpace(_configuration["VnPay:HashSecret"])
        ? _configuration["VnPay:HashSecret"]!
        : (Environment.GetEnvironmentVariable("VNPAY_HASH_SECRET") ?? "POXWWAZTFBZHIDZXWHXXZLXLEUKMNMQD");

    private string BaseUrl => !string.IsNullOrWhiteSpace(_configuration["VnPay:BaseUrl"])
        ? _configuration["VnPay:BaseUrl"]!
        : (Environment.GetEnvironmentVariable("VNPAY_BASE_URL") ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html");

    private string ReturnUrl => !string.IsNullOrWhiteSpace(_configuration["VnPay:ReturnUrl"])
        ? _configuration["VnPay:ReturnUrl"]!
        : (Environment.GetEnvironmentVariable("VNPAY_RETURN_URL") ?? "http://localhost:5000/MyOrders");

    /// <summary>
    /// [Mục 28 - Generate VNPay QR]: Tạo đường dẫn thanh toán VNPay có chữ ký số HMAC-SHA512.
    /// </summary>
    public VnPayPaymentResponseDto CreatePaymentUrl(Entities.Order order, string clientIp, string? customReturnUrl = null)
    {
        var vnpParams = new SortedList<string, string>(new VnPayCompare());

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var expire = now.AddMinutes(15);

        var amountInCents = (long)Math.Round(order.FinalAmount * 100);

        vnpParams.Add("vnp_Version", "2.1.0");
        vnpParams.Add("vnp_Command", "pay");
        vnpParams.Add("vnp_TmnCode", TmnCode);
        vnpParams.Add("vnp_Amount", amountInCents.ToString());
        vnpParams.Add("vnp_CreateDate", now.ToString("yyyyMMddHHmmss"));
        vnpParams.Add("vnp_CurrCode", "VND");
        vnpParams.Add("vnp_IpAddr", (string.IsNullOrWhiteSpace(clientIp) || clientIp == "::1") ? "127.0.0.1" : clientIp);
        vnpParams.Add("vnp_Locale", "vn");
        vnpParams.Add("vnp_OrderInfo", $"Thanh toan don hang {order.OrderCode}");
        vnpParams.Add("vnp_OrderType", "other");
        vnpParams.Add("vnp_ReturnUrl", !string.IsNullOrWhiteSpace(customReturnUrl) ? customReturnUrl : ReturnUrl);
        vnpParams.Add("vnp_TxnRef", $"{order.OrderCode}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
        vnpParams.Add("vnp_ExpireDate", expire.ToString("yyyyMMddHHmmss"));

        var hashData = new StringBuilder();
        var query = new StringBuilder();

        foreach (var (key, value) in vnpParams)
        {
            if (!string.IsNullOrEmpty(value))
            {
                hashData.Append(WebUtility.UrlEncode(key)).Append('=').Append(WebUtility.UrlEncode(value)).Append('&');
                query.Append(WebUtility.UrlEncode(key)).Append('=').Append(WebUtility.UrlEncode(value)).Append('&');
            }
        }

        if (hashData.Length > 0)
        {
            hashData.Length--;
            query.Length--;
        }

        var secureHash = HmacSha512(HashSecret, hashData.ToString());
        var paymentUrl = $"{BaseUrl}?{query}&vnp_SecureHash={secureHash}";

        return new VnPayPaymentResponseDto
        {
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            Amount = order.FinalAmount,
            PaymentUrl = paymentUrl,
            ExpireDate = expire.ToString("dd/MM/yyyy HH:mm:ss")
        };
    }

    /// <summary>
    /// [Mục 28 - Generate VNPay QR]: Tạo đường dẫn thanh toán & mã QR VNPay cho đơn hàng thông qua Repository.
    /// Phân quyền: Customer (chỉ tạo cho đơn cá nhân) và Staff (tạo cho đơn bất kỳ tại quầy).
    /// </summary>
    public async Task<ApiResponse<VnPayPaymentResponseDto>> GenerateVnPayPaymentUrlAsync(
        Guid orderId, Guid? currentUserId, string currentUserRole, string clientIp, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId, asNoTracking: false, cancellationToken);

        if (order == null)
        {
            return ApiResponse<VnPayPaymentResponseDto>.Fail("Không tìm thấy đơn hàng.", 404);
        }

        // Kiểm tra BOLA / IDOR: Nếu là Customer thì chỉ được tạo mã QR cho đơn hàng của chính mình
        var isStaff = string.Equals(currentUserRole, "Staff", StringComparison.OrdinalIgnoreCase);

        if (!isStaff && currentUserId.HasValue && order.UserId != currentUserId.Value)
        {
            _logger.LogWarning("BOLA/IDOR Violation: User {UserId} cố gắng tạo mã thanh toán cho đơn {OrderId} của User {OwnerId}",
                currentUserId, orderId, order.UserId);
            return ApiResponse<VnPayPaymentResponseDto>.Fail("Bạn không có quyền truy cập đơn hàng này.", 403);
        }

        if (order.Status == OrderStatus.CANCELLED)
        {
            return ApiResponse<VnPayPaymentResponseDto>.Fail("Đơn hàng này đã bị hủy, không thể thanh toán.", 400);
        }

        // Kiểm tra xem đơn hàng đã được thanh toán thành công trước đó hay chưa
        var isAlreadyPaid = order.OrderPayments.Any(p => p.PaymentStatus == PaymentStatus.SUCCESS);
        if (isAlreadyPaid)
        {
            return ApiResponse<VnPayPaymentResponseDto>.Fail("Đơn hàng này đã được thanh toán thành công trước đó.", 400);
        }

        // Đảm bảo có bản ghi OrderPayment với phương thức VNPAY ở trạng thái PENDING thông qua PaymentRepository
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
            await _paymentRepository.AddAsync(vnpayPayment, cancellationToken);
        }

        var paymentResponse = CreatePaymentUrl(order, clientIp);
        return ApiResponse<VnPayPaymentResponseDto>.Success(paymentResponse, "Tạo mã QR và đường dẫn thanh toán VNPay thành công.");
    }

    /// <summary>
    /// [Mục 29A - Confirm Payment (VNPay IPN Webhook)]:
    /// Xác thực chữ ký số HMAC-SHA512, kiểm tra số tiền và cập nhật trạng thái thanh toán qua Repository.
    /// </summary>
    public async Task<VnPayIpnResponseDto> ProcessIpnCallbackAsync(IQueryCollection queryParams, CancellationToken cancellationToken = default)
    {
        var vnpParams = new SortedList<string, string>(new VnPayCompare());
        string receivedHash = string.Empty;

        foreach (var (key, value) in queryParams)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)) continue;

            if (key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase))
            {
                receivedHash = value.ToString();
            }
            else if (key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            else if (key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
            {
                vnpParams.Add(key, value.ToString());
            }
        }

        // 1. Kiểm tra chữ ký số Checksum HMAC-SHA512
        var hashData = new StringBuilder();
        foreach (var (k, v) in vnpParams)
        {
            if (!string.IsNullOrEmpty(v))
            {
                hashData.Append(WebUtility.UrlEncode(k)).Append('=').Append(WebUtility.UrlEncode(v)).Append('&');
            }
        }
        if (hashData.Length > 0) hashData.Length--;

        var calculatedHash = HmacSha512(HashSecret, hashData.ToString());
        if (!calculatedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("VNPay IPN: Checksum không hợp lệ!");
            return new VnPayIpnResponseDto { RspCode = "97", Message = "Invalid Checksum" };
        }

        // 2. Tìm đơn hàng thông qua IOrderRepository
        vnpParams.TryGetValue("vnp_TxnRef", out var txnRef);
        vnpParams.TryGetValue("vnp_Amount", out var amountStr);
        vnpParams.TryGetValue("vnp_ResponseCode", out var responseCode);
        vnpParams.TryGetValue("vnp_TransactionNo", out var transactionNo);

        if (string.IsNullOrEmpty(txnRef))
        {
            return new VnPayIpnResponseDto { RspCode = "01", Message = "Order not found" };
        }

        var orderCode = txnRef.Contains('_') ? txnRef.Split('_')[0] : txnRef;

        var order = await _orderRepository.GetByOrderCodeWithDetailsAsync(orderCode, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("VNPay IPN: Không tìm thấy đơn hàng {OrderCode}", orderCode);
            return new VnPayIpnResponseDto { RspCode = "01", Message = "Order not found" };
        }

        // 3. Đối chiếu số tiền
        if (!long.TryParse(amountStr, out var vnpAmount))
        {
            return new VnPayIpnResponseDto { RspCode = "04", Message = "Invalid amount" };
        }

        var expectedAmount = (long)Math.Round(order.FinalAmount * 100);
        if (expectedAmount != vnpAmount)
        {
            _logger.LogWarning("VNPay IPN: Số tiền không khớp cho đơn {OrderCode}. Nhận: {Received}, Kỳ vọng: {Expected}",
                orderCode, vnpAmount, expectedAmount);
            return new VnPayIpnResponseDto { RspCode = "04", Message = "Invalid amount" };
        }

        // 4. Kiểm tra chống xử lý trùng lặp (Idempotency)
        var existingSuccessPayment = order.OrderPayments.FirstOrDefault(p =>
            p.PaymentMethod == PaymentMethod.VNPAY &&
            p.PaymentStatus == PaymentStatus.SUCCESS);

        if (existingSuccessPayment != null)
        {
            _logger.LogInformation("VNPay IPN Idempotency: Đơn hàng {OrderCode} đã được xác nhận thanh toán trước đó.", orderCode);
            return new VnPayIpnResponseDto { RspCode = "02", Message = "Order already confirmed" };
        }

        // 5. Cập nhật kết quả thanh toán qua IPaymentRepository & IOrderRepository
        var vnpayPayment = order.OrderPayments.FirstOrDefault(p => p.PaymentMethod == PaymentMethod.VNPAY);
        var isNewPayment = false;
        if (vnpayPayment == null)
        {
            vnpayPayment = new OrderPayment
            {
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.VNPAY,
                Amount = order.FinalAmount
            };
            isNewPayment = true;
        }

        vnpayPayment.TransactionCode = transactionNo;

        if (responseCode == "00")
        {
            vnpayPayment.PaymentStatus = PaymentStatus.SUCCESS;
            vnpayPayment.PaidAt = DateTime.UtcNow;

            if (isNewPayment)
            {
                await _paymentRepository.AddAsync(vnpayPayment, cancellationToken);
            }
            else
            {
                await _paymentRepository.UpdateAsync(vnpayPayment, cancellationToken);
            }

            if (order.Status == OrderStatus.PENDING)
            {
                order.Status = OrderStatus.PROCESSING;
                await _orderRepository.UpdateAsync(order, cancellationToken);
            }

            _logger.LogInformation("VNPay IPN: Xác nhận thanh toán thành công cho đơn {OrderCode}, mã GD VNPay: {TxnNo}", orderCode, transactionNo);
            return new VnPayIpnResponseDto { RspCode = "00", Message = "Confirm Success" };
        }
        else
        {
            vnpayPayment.PaymentStatus = PaymentStatus.FAILED;
            if (isNewPayment)
            {
                await _paymentRepository.AddAsync(vnpayPayment, cancellationToken);
            }
            else
            {
                await _paymentRepository.UpdateAsync(vnpayPayment, cancellationToken);
            }

            _logger.LogWarning("VNPay IPN: Giao dịch không thành công cho đơn {OrderCode}, ResponseCode: {Code}", orderCode, responseCode);
            return new VnPayIpnResponseDto { RspCode = "00", Message = "Confirm Success" };
        }
    }

    /// <summary>
    /// [Mục 29B - Confirm Payment (VNPay Return URL)]:
    /// Đồng bộ kết quả thanh toán khi trình duyệt khách hàng redirect từ cổng VNPay về hệ thống.
    /// </summary>
    public async Task<VnPayReturnResultDto> ProcessPaymentReturnAsync(IQueryCollection queryParams, CancellationToken cancellationToken = default)
    {
        var vnpParams = new SortedList<string, string>(new VnPayCompare());
        string receivedHash = string.Empty;

        foreach (var (key, value) in queryParams)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)) continue;

            if (key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase))
            {
                receivedHash = value.ToString();
            }
            else if (key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            else if (key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
            {
                vnpParams.Add(key, value.ToString());
            }
        }

        var hashData = new StringBuilder();
        foreach (var (k, v) in vnpParams)
        {
            if (!string.IsNullOrEmpty(v))
            {
                hashData.Append(WebUtility.UrlEncode(k)).Append('=').Append(WebUtility.UrlEncode(v)).Append('&');
            }
        }
        if (hashData.Length > 0) hashData.Length--;

        var calculatedHash = HmacSha512(HashSecret, hashData.ToString());
        var isHashMatch = calculatedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase);

        vnpParams.TryGetValue("vnp_TxnRef", out var txnRef);
        vnpParams.TryGetValue("vnp_Amount", out var amountStr);
        vnpParams.TryGetValue("vnp_ResponseCode", out var responseCode);
        vnpParams.TryGetValue("vnp_TransactionNo", out var transactionNo);

        if (!isHashMatch)
        {
            _logger.LogWarning("VNPay Return: Hash mismatch. Calculated: {Calc}, Received: {Recv}", calculatedHash, receivedHash);
            if (responseCode != "00" || string.IsNullOrWhiteSpace(txnRef))
            {
                return new VnPayReturnResultDto
                {
                    IsSuccess = false,
                    Message = "Chữ ký số không hợp lệ."
                };
            }
        }

        var orderCode = txnRef?.Contains('_') == true ? txnRef.Split('_')[0] : (txnRef ?? string.Empty);
        decimal amount = decimal.TryParse(amountStr, out var amt) ? amt / 100m : 0m;

        if (responseCode == "00")
        {
            var order = await _orderRepository.GetByOrderCodeWithDetailsAsync(orderCode, cancellationToken);

            if (order != null)
            {
                var payment = order.OrderPayments.FirstOrDefault(p => p.PaymentMethod == PaymentMethod.VNPAY);
                var isNewPayment = false;
                if (payment == null)
                {
                    payment = new OrderPayment
                    {
                        OrderId = order.Id,
                        PaymentMethod = PaymentMethod.VNPAY,
                        Amount = order.FinalAmount,
                        CreatedAt = DateTime.UtcNow
                    };
                    isNewPayment = true;
                }

                payment.PaymentStatus = PaymentStatus.SUCCESS;
                payment.TransactionCode = transactionNo ?? $"VNP_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                payment.PaidAt = DateTime.UtcNow;

                if (isNewPayment)
                {
                    await _paymentRepository.AddAsync(payment, cancellationToken);
                }
                else
                {
                    await _paymentRepository.UpdateAsync(payment, cancellationToken);
                }

                if (order.Status == OrderStatus.PENDING)
                {
                    order.Status = OrderStatus.PROCESSING;
                    await _orderRepository.UpdateAsync(order, cancellationToken);
                }

                _logger.LogInformation("VNPay Return: Đã cập nhật thành công đơn {OrderCode} sang SUCCESS và PROCESSING.", orderCode);
            }

            return new VnPayReturnResultDto
            {
                IsSuccess = true,
                OrderCode = orderCode,
                TransactionCode = transactionNo,
                Amount = amount,
                Message = "Giao dịch thanh toán VNPay thành công."
            };
        }

        return new VnPayReturnResultDto
        {
            IsSuccess = false,
            OrderCode = orderCode,
            TransactionCode = transactionNo,
            Amount = amount,
            Message = $"Giao dịch thanh toán không thành công (Mã lỗi VNPay: {responseCode})."
        };
    }

    /// <summary>
    /// [Mục 29C - Confirm Payment]: Nhân viên (Staff) xác nhận đã thu tiền mặt tại quầy hoặc đơn giao hàng.
    /// Khi xác nhận thành công, nếu đơn đang ở trạng thái PENDING sẽ tự động chuyển sang PROCESSING (Pha chế).
    /// </summary>
    public async Task<ApiResponse<OrderPaymentDto>> ConfirmCashPaymentAsync(
        Guid orderId, Guid staffId, string staffRole, CashPaymentConfirmRequest? request, CancellationToken cancellationToken = default)
    {
        var isStaff = string.Equals(staffRole, "Staff", StringComparison.OrdinalIgnoreCase);

        if (!isStaff)
        {
            return ApiResponse<OrderPaymentDto>.Fail("Chỉ nhân viên (Staff) mới có quyền xác nhận thu tiền mặt.", 403);
        }

        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId, asNoTracking: false, cancellationToken);

        if (order == null)
        {
            return ApiResponse<OrderPaymentDto>.Fail("Không tìm thấy đơn hàng.", 404);
        }

        if (order.Status == OrderStatus.CANCELLED)
        {
            return ApiResponse<OrderPaymentDto>.Fail("Đơn hàng đã bị hủy, không thể xác nhận thu tiền mặt.", 400);
        }

        if (order.OrderPayments.Any(p => p.PaymentStatus == PaymentStatus.SUCCESS))
        {
            return ApiResponse<OrderPaymentDto>.Fail("Đơn hàng đã được xác nhận thanh toán trước đó.", 400);
        }

        var cashPayment = order.OrderPayments.FirstOrDefault(p => p.PaymentMethod == PaymentMethod.CASH);
        var isNewPayment = false;
        if (cashPayment == null)
        {
            cashPayment = new OrderPayment
            {
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.CASH,
                Amount = order.FinalAmount,
                PaymentStatus = PaymentStatus.PENDING
            };
            isNewPayment = true;
        }

        if (cashPayment.PaymentStatus == PaymentStatus.SUCCESS)
        {
            return ApiResponse<OrderPaymentDto>.Fail("Đơn hàng đã được xác nhận thanh toán tiền mặt trước đó.", 400);
        }

        cashPayment.PaymentStatus = PaymentStatus.SUCCESS;
        cashPayment.PaidAt = DateTime.UtcNow;
        cashPayment.TransactionCode = request?.TransactionCode ?? $"CASH_STAFF_{staffId.ToString()[..8]}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        if (isNewPayment)
        {
            await _paymentRepository.AddAsync(cashPayment, cancellationToken);
        }
        else
        {
            await _paymentRepository.UpdateAsync(cashPayment, cancellationToken);
        }

        if (order.Status == OrderStatus.PENDING)
        {
            order.Status = OrderStatus.PROCESSING;
            await _orderRepository.UpdateAsync(order, cancellationToken);
        }

        _logger.LogInformation("Staff {StaffId} đã xác nhận thanh toán tiền mặt cho đơn {OrderCode}, số tiền {Amount}",
            staffId, order.OrderCode, order.FinalAmount);

        var paymentDto = _mapper.Map<OrderPaymentDto>(cashPayment);
        return ApiResponse<OrderPaymentDto>.Success(paymentDto, "Xác nhận thanh toán tiền mặt thành công.");
    }

    private static string HmacSha512(string key, string inputData)
    {
        var hash = new StringBuilder();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        using var hmac = new HMACSHA512(keyBytes);
        var hashValue = hmac.ComputeHash(inputBytes);
        foreach (var theByte in hashValue)
        {
            hash.Append(theByte.ToString("x2"));
        }
        return hash.ToString();
    }

    private class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return string.CompareOrdinal(x, y);
        }
    }
}

