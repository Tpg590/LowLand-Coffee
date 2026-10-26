using System.Diagnostics;

namespace ApiGateway.Middleware;

/// <summary>
/// Middleware ghi nhật ký (Logging) tập trung cho mọi yêu cầu đi qua API Gateway,
/// đo lường thời gian phản hồi (latency) và giám sát trạng thái HTTP.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        _logger.LogInformation("--> Gateway INCOMING: {Method} {Path} from {ClientIp}",
            method, path, clientIp);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (statusCode >= 500)
            {
                _logger.LogError("<-- Gateway OUTGOING: {Method} {Path} -> {StatusCode} ({ElapsedMs} ms)",
                    method, path, statusCode, elapsedMs);
            }
            else if (statusCode >= 400)
            {
                _logger.LogWarning("<-- Gateway OUTGOING: {Method} {Path} -> {StatusCode} ({ElapsedMs} ms)",
                    method, path, statusCode, elapsedMs);
            }
            else
            {
                _logger.LogInformation("<-- Gateway OUTGOING: {Method} {Path} -> {StatusCode} ({ElapsedMs} ms)",
                    method, path, statusCode, elapsedMs);
            }
        }
    }
}
