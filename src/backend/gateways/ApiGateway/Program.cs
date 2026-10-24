using System.Diagnostics;
using ApiGateway.Extensions;
using ApiGateway.Middleware;
using ApiGateway.Models;

var builder = WebApplication.CreateBuilder(args);
var startTime = DateTime.UtcNow;

// Nạp file cấu hình YARP routes & clusters riêng biệt
builder.Configuration.AddJsonFile("yarp.json", optional: false, reloadOnChange: true);

// Nạp tự động các file cấu hình *.ratelimit.json của từng Service
var rateLimitsPath = Path.Combine(builder.Environment.ContentRootPath, "RateLimits");
if (Directory.Exists(rateLimitsPath))
{
    foreach (var file in Directory.GetFiles(rateLimitsPath, "*.ratelimit.json"))
    {
        builder.Configuration.AddJsonFile(file, optional: false, reloadOnChange: true);
    }
}

// Nạp lại Environment Variables để biến môi trường từ Docker/OS có độ ưu tiên cao nhất
builder.Configuration.AddEnvironmentVariables();

// 1. Đăng ký YARP Reverse Proxy nạp cấu hình từ yarp.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 2. Đăng ký Chính sách CORS tập trung (hỗ trợ Web client & SignalR credentials)
builder.Services.AddGatewayCors(builder.Configuration);

// 3. Đăng ký Rate Limiting chi tiết theo từng Endpoint từ các file JSON
builder.Services.AddGatewayRateLimiting(builder.Configuration);

// 4. Đăng ký Health Checks
builder.Services.AddHealthChecks();

// 5. Đăng ký Swagger UI cho Gateway (Tập hợp OpenAPI Docs của 5 Microservices)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// =========================================================================
// Cấu hình HTTP Request Pipeline theo thứ tự tối ưu
// =========================================================================

// 1. Quản lý Correlation ID (Distributed Tracing)
app.UseMiddleware<CorrelationIdMiddleware>();

// 2. Ghi nhật ký tập trung & Đo lường độ trễ (Centralized Request Logging)
app.UseMiddleware<RequestLoggingMiddleware>();

// 3. Xử lý ngoại lệ và chuẩn hóa lỗi kết nối downstream (502/503/504 -> ApiResponse)
app.UseMiddleware<GatewayResponseHandlerMiddleware>();

// 4. Áp dụng CORS Policy
app.UseCors(GatewayServiceExtensions.CorsPolicyName);

// 5. Áp dụng Rate Limiter
app.UseRateLimiter();

// 6. Kiểm tra tính hợp lệ của JWT Bearer Token & trích xuất claims forward downstream
app.UseMiddleware<JwtValidationMiddleware>();

// 7. Cổng Swagger UI hợp nhất (Unified Swagger Portal)
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "LowLand Coffee - API Gateway Documentation";
    options.SwaggerEndpoint("/swagger-docs/auth/v1/swagger.json", "1. Auth & Identity API (Khang)");
    options.SwaggerEndpoint("/swagger-docs/product/v1/swagger.json", "2. Product & Catalog API (Phú)");
    options.SwaggerEndpoint("/swagger-docs/order/v1/swagger.json", "3. Order & Payment API (Tuấn)");
    options.SwaggerEndpoint("/swagger-docs/engagement/v1/swagger.json", "4. Engagement & Loyalty API (Trí)");
    options.SwaggerEndpoint("/swagger-docs/notification/v1/swagger.json", "5. Notification & Realtime API (Thành)");
});

// 8. Health Check endpoint chuẩn ASP.NET Core
app.MapHealthChecks("/health");

// 9. Root Health & Diagnostics Endpoint
app.MapGet("/", (IConfiguration config) =>
{
    var proxySection = config.GetSection("ReverseProxy");
    var routes = proxySection.GetSection("Routes")
        .GetChildren()
        .Select(r => $"{r.Key} => {r["Match:Path"]}")
        .ToList();

    var clusters = proxySection.GetSection("Clusters")
        .GetChildren()
        .ToDictionary(
            c => c.Key,
            c => c["Destinations:destination1:Address"] ?? "unknown"
        );

    var response = new GatewayStatusResponse
    {
        Service = "LowLand Coffee YARP API Gateway",
        Status = "Healthy",
        Version = "1.0.0",
        Environment = app.Environment.EnvironmentName,
        ServerTimeUtc = DateTime.UtcNow,
        Uptime = DateTime.UtcNow - startTime,
        Clusters = clusters,
        Routes = routes,
        Features =
        [
            "YARP Reverse Proxy 2.1",
            "Centralized CORS with Credentials",
            "Fixed-Window Rate Limiting (General & Auth Policies)",
            "Distributed Tracing (X-Correlation-Id)",
            "JWT Token Pre-validation & Header Forwarding",
            "Unified Swagger UI Portal (/swagger)",
            "Health Check Endpoint (/health)"
        ]
    };

    return Results.Ok(response);
});

// 10. Map YARP Reverse Proxy
app.MapReverseProxy();

app.Run();
