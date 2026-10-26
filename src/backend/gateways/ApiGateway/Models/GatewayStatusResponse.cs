namespace ApiGateway.Models;

/// <summary>
/// Model phản hồi thông tin sức khỏe và cấu hình Gateway
/// </summary>
public class GatewayStatusResponse
{
    public string Service { get; set; } = "LowLand Coffee YARP API Gateway";
    public string Status { get; set; } = "Healthy";
    public string Version { get; set; } = "1.0.0";
    public string Environment { get; set; } = string.Empty;
    public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
    public TimeSpan Uptime { get; set; }
    public Dictionary<string, string> Clusters { get; set; } = new();
    public List<string> Routes { get; set; } = new();
    public List<string> Features { get; set; } = new();
}
