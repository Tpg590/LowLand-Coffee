namespace Order.API.Entities;

public static class PaymentMethod
{
    public const string VNPAY = "VNPAY";
    public const string CASH = "CASH";
    public const string PAYOS = "PAYOS";

    public static readonly string[] ValidMethods = [VNPAY, CASH, PAYOS];

    /// <summary>
    /// Chuẩn hóa phương thức thanh toán từ cả Tiếng Việt và Tiếng Anh
    /// </summary>
    public static string Normalize(string? method)
    {
        if (string.IsNullOrWhiteSpace(method)) return CASH;
        var m = method.Trim().ToUpperInvariant();
        return m switch
        {
            "TIỀN MẶT" or "TIEN MAT" or "TIEN_MAT" or "CASH" => CASH,
            "VNPAY" or "VN_PAY" or "QUÉT MÃ" or "QUET MA" or "QR" or "CHUYỂN KHOẢN" or "CHUYEN KHOAN" => VNPAY,
            "PAYOS" => PAYOS,
            _ => m
        };
    }

    public static bool IsValid(string? method)
    {
        if (string.IsNullOrWhiteSpace(method)) return false;
        var normalized = Normalize(method);
        return ValidMethods.Contains(normalized);
    }
}
