using System.Threading.RateLimiting;
using ApiGateway.Models;
using Microsoft.AspNetCore.RateLimiting;

namespace ApiGateway.Extensions;

/// <summary>
/// Các phương thức mở rộng (Extension Methods) cấu hình Services cho API Gateway
/// </summary>
public static class GatewayServiceExtensions
{
    public const string CorsPolicyName = "GatewayCorsPolicy";

    /// <summary>
    /// Đăng ký chính sách CORS tập trung cho API Gateway
    /// </summary>
    public static IServiceCollection AddGatewayCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

                if (configuredOrigins != null && configuredOrigins.Length > 0)
                {
                    policy.WithOrigins(configuredOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
                else
                {
                    policy.SetIsOriginAllowed(_ => true)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }

                policy.WithExposedHeaders("X-Correlation-Id", "Content-Disposition", "X-Pagination");
            });
        });

        return services;
    }

    /// <summary>
    /// Đăng ký Rate Limiting chi tiết theo từng Endpoint nạp động từ các file *.ratelimit.json
    /// </summary>
    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Đọc rule mặc định (Default fallback)
        var defaultRule = configuration.GetSection("RateLimiting:Default").Get<RateLimitRule>()
                          ?? new RateLimitRule { PermitLimit = 100, WindowSeconds = 60 };

        // 2. Thu thập toàn bộ rules của các service từ section "RateLimiting"
        var rateLimitingSection = configuration.GetSection("RateLimiting");
        var endpointRules = new List<RateLimitRule>();

        foreach (var serviceSection in rateLimitingSection.GetChildren())
        {
            if (string.Equals(serviceSection.Key, "Default", StringComparison.OrdinalIgnoreCase))
                continue;

            var rules = serviceSection.Get<List<RateLimitRule>>();
            if (rules != null && rules.Count > 0)
            {
                endpointRules.AddRange(rules);
            }
        }

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Xử lý khi request vượt quá hạn mức cho phép
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                var response = ApiResponse<object>.Fail(
                    message: "Quá nhiều yêu cầu gửi tới endpoint này cùng lúc. Vui lòng thử lại sau.",
                    statusCode: StatusCodes.Status429TooManyRequests,
                    errors: ["Rate limit exceeded for this endpoint. Please try again later."]
                );
                await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            };

            // 3. Áp dụng GlobalLimiter phân vùng theo IP + Endpoint
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var path = httpContext.Request.Path.Value?.ToLowerInvariant() ?? "";
                var method = httpContext.Request.Method.ToUpperInvariant();
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                // Bỏ qua giới hạn tốc độ cho tài nguyên tĩnh của Frontend (CSS, JS, Fonts, Images)
                if (path.EndsWith(".css") || path.EndsWith(".js") || path.EndsWith(".png") ||
                    path.EndsWith(".jpg") || path.EndsWith(".jpeg") || path.EndsWith(".svg") ||
                    path.EndsWith(".ico") || path.EndsWith(".woff") || path.EndsWith(".woff2"))
                {
                    return RateLimitPartition.GetNoLimiter("static_assets");
                }

                // Khớp rule theo Path và HTTP Method
                var matchedRule = endpointRules.FirstOrDefault(r =>
                {
                    var isMethodMatch = r.Method == "*" || string.Equals(r.Method, method, StringComparison.OrdinalIgnoreCase);
                    var cleanRulePath = r.Path.TrimEnd('/').ToLowerInvariant();
                    var isPathMatch = string.Equals(cleanRulePath, path, StringComparison.OrdinalIgnoreCase) ||
                                      path.StartsWith(cleanRulePath + "/", StringComparison.OrdinalIgnoreCase);

                    return isMethodMatch && isPathMatch;
                });

                var rule = matchedRule ?? defaultRule;
                var partitionKey = matchedRule != null
                    ? $"{clientIp}_{matchedRule.Method}_{matchedRule.Path.ToLowerInvariant()}"
                    : $"{clientIp}_default";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rule.PermitLimit,
                        Window = TimeSpan.FromSeconds(rule.WindowSeconds),
                        QueueLimit = rule.QueueLimit
                    });
            });
        });

        return services;
    }
}
