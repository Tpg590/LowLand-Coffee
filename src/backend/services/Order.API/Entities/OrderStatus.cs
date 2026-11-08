namespace Order.API.Entities;

public static class OrderStatus
{
    public const string PENDING = "PENDING";         // Tiếp nhận / Chờ xử lý
    public const string PROCESSING = "PROCESSING";   // Đang pha chế
    public const string SHIPPING = "SHIPPING";       // Đang giao hàng
    public const string COMPLETED = "COMPLETED";     // Hoàn thành
    public const string CANCELLED = "CANCELLED";     // Đã hủy

    public static readonly string[] ValidStatuses = [PENDING, PROCESSING, SHIPPING, COMPLETED, CANCELLED];

    /// <summary>
    /// Chuẩn hóa trạng thái từ cả Tiếng Việt (có dấu hoặc không dấu) và Tiếng Anh
    /// </summary>
    public static string Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return string.Empty;
        var s = status.Trim().ToUpperInvariant();
        return s switch
        {
            "TIẾP NHẬN" or "TIEP NHAN" or "PENDING" or "CHỜ XỬ LÝ" or "CHO XU LY" or "CONFIRMED" or "ĐÃ TIẾP NHẬN" or "DA TIEP NHAN" or "ĐÃ XÁC NHẬN" or "DA XAC NHAN" => PENDING,
            "PHA CHẾ" or "PHA CHE" or "ĐANG PHA CHẾ" or "DANG PHA CHE" or "PROCESSING" or "PREPARING" or "CHUẨN BỊ" or "DANG CHUAN BI" => PROCESSING,
            "GIAO HÀNG" or "GIAO HANG" or "ĐANG GIAO HÀNG" or "DANG GIAO HANG" or "SHIPPING" or "DELIVERING" or "DELIVERY" or "READY" or "SẴN SÀNG" or "SAN SANG" => SHIPPING,
            "HOÀN THÀNH" or "HOAN THANH" or "COMPLETED" or "XONG" or "DONE" or "FINISHED" => COMPLETED,
            "HỦY" or "ĐÃ HỦY" or "DA HUY" or "HUY" or "HỦY ĐƠN" or "HUY DON" or "CANCELLED" or "CANCEL" => CANCELLED,
            _ => s
        };
    }

    public static bool IsValid(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;
        var normalized = Normalize(status);
        return ValidStatuses.Contains(normalized);
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ của việc chuyển đổi trạng thái:
    /// PENDING -> PROCESSING -> SHIPPING -> COMPLETED
    /// </summary>
    public static bool CanTransition(string currentStatus, string nextStatus)
    {
        var current = Normalize(currentStatus);
        var next = Normalize(nextStatus);

        if (current == next) return true;

        return current switch
        {
            PENDING => next == PROCESSING || next == CANCELLED,
            PROCESSING => next == SHIPPING || next == CANCELLED,
            SHIPPING => next == COMPLETED || next == CANCELLED,
            COMPLETED => false,
            CANCELLED => false,
            _ => false
        };
    }
}
