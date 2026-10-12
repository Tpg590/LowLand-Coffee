using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Order.API.Data;
using Order.API.DTOs;
using Order.API.Entities;
using Order.API.Hubs;
using Order.API.Services.Interfaces;
using QRCoder;

namespace Order.API.Services.Implementations;

public class VnPayService : IVnPayService
{
    private readonly IConfiguration _configuration;
    private readonly OrderDbContext _dbContext;
    private readonly IOrderNotificationService _notificationService;
    private readonly ILogger<VnPayService> _logger;

    public VnPayService(
        IConfiguration configuration,
        OrderDbContext dbContext,
        IOrderNotificationService notificationService,
        ILogger<VnPayService> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    private string TmnCode => _configuration["VnPay:TmnCode"]
        ?? Environment.GetEnvironmentVariable("VNPAY_TMN_CODE")
        ?? string.Empty;

    private string HashSecret => _configuration["VnPay:HashSecret"]
        ?? Environment.GetEnvironmentVariable("VNPAY_HASH_SECRET")
        ?? string.Empty;

    private string BaseUrl => _configuration["VnPay:BaseUrl"]
        ?? Environment.GetEnvironmentVariable("VNPAY_BASE_URL")
        ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    private string ReturnUrl => _configuration["VnPay:ReturnUrl"]
        ?? Environment.GetEnvironmentVariable("VNPAY_RETURN_URL")
        ?? "http://localhost:5000/api/payments/vnpay-return";

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
        vnpParams.Add("vnp_IpAddr", string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp);
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
            hashData.Length--; // Bỏ dấu & cuối cùng
            query.Length--;
        }

        var secureHash = HmacSha512(HashSecret, hashData.ToString());
        var paymentUrl = $"{BaseUrl}?{query}&vnp_SecureHash={secureHash}";

        // Tạo mã QR bằng QRCoder (PngByteQRCode không phụ thuộc System.Drawing cross-platform)
        string? qrCodeBase64 = null;
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(paymentUrl, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);
            qrCodeBase64 = $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể tạo mã QR hình ảnh cho đơn hàng {OrderCode}", order.OrderCode);
        }

        return new VnPayPaymentResponseDto
        {
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            Amount = order.FinalAmount,
            PaymentUrl = paymentUrl,
            QrCodeBase64 = qrCodeBase64,
            ExpireDate = expire.ToString("dd/MM/yyyy HH:mm:ss")
        };
    }

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
            else if (key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
            {
                vnpParams.Add(key, value.ToString());
            }
        }

        // 1. Kiểm tra chữ ký Checksum
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

        // 2. Tìm đơn hàng
        vnpParams.TryGetValue("vnp_TxnRef", out var txnRef);
        vnpParams.TryGetValue("vnp_Amount", out var amountStr);
        vnpParams.TryGetValue("vnp_ResponseCode", out var responseCode);
        vnpParams.TryGetValue("vnp_TransactionNo", out var transactionNo);

        if (string.IsNullOrEmpty(txnRef))
        {
            return new VnPayIpnResponseDto { RspCode = "01", Message = "Order not found" };
        }

        // txnRef dạng ORD-xxx hoặc ORD-xxx_timestamp
        var orderCode = txnRef.Contains('_') ? txnRef.Split('_')[0] : txnRef;

        var order = await _dbContext.Orders
            .Include(o => o.OrderPayments)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("VNPay IPN: Không tìm thấy đơn hàng {OrderCode}", orderCode);
            return new VnPayIpnResponseDto { RspCode = "01", Message = "Order not found" };
        }

        // 3. Kiểm tra số tiền
        if (!long.TryParse(amountStr, out var vnpAmount))
        {
            return new VnPayIpnResponseDto { RspCode = "04", Message = "Invalid amount" };
        }

        var expectedAmount = (long)Math.Round(order.FinalAmount * 100);
        if (expectedAmount != vnpAmount)
        {
            _logger.LogWarning("VNPay IPN: Số tiền không khớp cho đơn {OrderCode}. Nhận: {Received}, Kì vọng: {Expected}",
                orderCode, vnpAmount, expectedAmount);
            return new VnPayIpnResponseDto { RspCode = "04", Message = "Invalid amount" };
        }

        // 4. Kiểm tra Idempotency (Trùng lặp giao dịch)
        var existingSuccessPayment = order.OrderPayments.FirstOrDefault(p =>
            p.PaymentMethod == Entities.PaymentMethod.VNPAY &&
            p.PaymentStatus == Entities.PaymentStatus.SUCCESS);

        if (existingSuccessPayment != null)
        {
            _logger.LogInformation("VNPay IPN Idempotency: Đơn hàng {OrderCode} đã được xác nhận thanh toán trước đó.", orderCode);
            return new VnPayIpnResponseDto { RspCode = "02", Message = "Order already confirmed" };
        }

        // 5. Cập nhật kết quả thanh toán
        var vnpayPayment = order.OrderPayments.FirstOrDefault(p => p.PaymentMethod == Entities.PaymentMethod.VNPAY)
                           ?? new OrderPayment
                           {
                               OrderId = order.Id,
                               PaymentMethod = Entities.PaymentMethod.VNPAY,
                               Amount = order.FinalAmount
                           };

        if (!_dbContext.OrderPayments.Contains(vnpayPayment))
        {
            await _dbContext.OrderPayments.AddAsync(vnpayPayment, cancellationToken);
        }

        vnpayPayment.TransactionCode = transactionNo;

        if (responseCode == "00")
        {
            vnpayPayment.PaymentStatus = Entities.PaymentStatus.SUCCESS;
            vnpayPayment.PaidAt = DateTime.UtcNow;

            // Nếu đơn còn ở PENDING thì chuyển lên PROCESSING
            if (order.Status == OrderStatus.PENDING)
            {
                order.Status = OrderStatus.PROCESSING;
                order.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Bắn SignalR realtime
            await _notificationService.NotifyPaymentConfirmedAsync(
                order.Id, order.OrderCode, order.UserId, Entities.PaymentMethod.VNPAY, Entities.PaymentStatus.SUCCESS, order.FinalAmount);

            if (order.Status == OrderStatus.PROCESSING)
            {
                await _notificationService.NotifyOrderStatusChangedAsync(
                    order.Id, order.OrderCode, order.UserId, order.Status);
            }

            _logger.LogInformation("VNPay IPN: Xác nhận thanh toán thành công cho đơn {OrderCode}, mã GD VNPay: {TxnNo}", orderCode, transactionNo);
            return new VnPayIpnResponseDto { RspCode = "00", Message = "Confirm Success" };
        }
        else
        {
            vnpayPayment.PaymentStatus = Entities.PaymentStatus.FAILED;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("VNPay IPN: Giao dịch không thành công cho đơn {OrderCode}, ResponseCode: {Code}", orderCode, responseCode);
            return new VnPayIpnResponseDto { RspCode = "00", Message = "Confirm Success" };
        }
    }

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
        if (!calculatedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase))
        {
            return new VnPayReturnResultDto
            {
                IsSuccess = false,
                Message = "Chữ ký số không hợp lệ."
            };
        }

        vnpParams.TryGetValue("vnp_TxnRef", out var txnRef);
        vnpParams.TryGetValue("vnp_Amount", out var amountStr);
        vnpParams.TryGetValue("vnp_ResponseCode", out var responseCode);
        vnpParams.TryGetValue("vnp_TransactionNo", out var transactionNo);

        var orderCode = txnRef?.Contains('_') == true ? txnRef.Split('_')[0] : (txnRef ?? string.Empty);
        decimal amount = decimal.TryParse(amountStr, out var amt) ? amt / 100m : 0m;

        if (responseCode == "00")
        {
            // Cập nhật DB nếu IPN chưa kịp chạy
            var order = await _dbContext.Orders
                .Include(o => o.OrderPayments)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode, cancellationToken);

            if (order != null)
            {
                var payment = order.OrderPayments.FirstOrDefault(p => p.PaymentMethod == Entities.PaymentMethod.VNPAY);
                if (payment != null && payment.PaymentStatus != Entities.PaymentStatus.SUCCESS)
                {
                    payment.PaymentStatus = Entities.PaymentStatus.SUCCESS;
                    payment.TransactionCode = transactionNo;
                    payment.PaidAt = DateTime.UtcNow;

                    if (order.Status == OrderStatus.PENDING)
                    {
                        order.Status = OrderStatus.PROCESSING;
                        order.UpdatedAt = DateTime.UtcNow;
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    await _notificationService.NotifyPaymentConfirmedAsync(
                        order.Id, order.OrderCode, order.UserId, Entities.PaymentMethod.VNPAY, Entities.PaymentStatus.SUCCESS, order.FinalAmount);
                }
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
