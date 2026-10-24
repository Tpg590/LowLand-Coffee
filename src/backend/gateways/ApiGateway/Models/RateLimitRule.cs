namespace ApiGateway.Models;

/// <summary>
/// Model cấu hình quy tắc Rate Limit chi tiết cho từng Endpoint
/// </summary>
public class RateLimitRule
{
    /// <summary>
    /// Đường dẫn endpoint (ví dụ: /api/auth/login, /api/orders)
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Phương thức HTTP (GET, POST, PUT, DELETE, hoặc "*" cho tất cả)
    /// </summary>
    public string Method { get; set; } = "*";

    /// <summary>
    /// Số lượng request tối đa được phép trong một cửa sổ thời gian
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Độ dài cửa sổ thời gian tính bằng giây (mặc định 60 giây = 1 phút)
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Số lượng request xếp hàng chờ xử lý nếu vượt ngưỡng (mặc định 0 = từ chối ngay)
    /// </summary>
    public int QueueLimit { get; set; } = 0;
}
