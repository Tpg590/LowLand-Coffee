namespace ApiGateway.Middleware;

/// <summary>
/// Middleware đảm bảo mỗi HTTP request đi qua Gateway đều gắn kèm Correlation ID
/// hỗ trợ kiểm vết (Distributed Tracing) xuyên suốt giữa Gateway và 5 Microservices backend.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string CorrelationIdHeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Kiểm tra hoặc sinh mới Correlation ID
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) ||
            string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
            context.Request.Headers[CorrelationIdHeaderName] = correlationId;
        }

        // Lưu vào Items của HttpContext để sử dụng nội bộ nếu cần
        context.Items[CorrelationIdHeaderName] = correlationId.ToString();

        // 2. Gắn Correlation ID vào Response Header trả về cho Client
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
            {
                context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            }
            return Task.CompletedTask;
        });

        // 3. Đưa Correlation ID vào phạm vi log có cấu trúc (Structured Logging Scope)
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId.ToString()
        }))
        {
            await _next(context);
        }
    }
}
