using System.Text.Json;
using ApiGateway.Models;
using Yarp.ReverseProxy.Forwarder;

namespace ApiGateway.Middleware;

/// <summary>
/// Middleware xử lý ngoại lệ tập trung và chuyển đổi lỗi kết nối downstream (502, 503, 504)
/// thành cấu trúc phản hồi chuẩn ApiResponse<T> của hệ thống LowLand Coffee.
/// </summary>
public class GatewayResponseHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayResponseHandlerMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GatewayResponseHandlerMiddleware(RequestDelegate next, ILogger<GatewayResponseHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            // Kiểm tra lỗi chuyển tiếp từ YARP Reverse Proxy Forwarder
            var forwarderError = context.Features.Get<IForwarderErrorFeature>();
            if (forwarderError != null && !context.Response.HasStarted)
            {
                _logger.LogError("YARP Forwarder Error: {Error} - Path: {Path}",
                    forwarderError.Error, context.Request.Path);

                var (statusCode, message) = forwarderError.Error switch
                {
                    ForwarderError.NoAvailableDestinations =>
                        (StatusCodes.Status503ServiceUnavailable, "Không có instance nào của Microservice đang khả dụng."),
                    ForwarderError.RequestTimedOut =>
                        (StatusCodes.Status504GatewayTimeout, "Yêu cầu tới Microservice nội bộ đã hết hạn thời gian (Timeout)."),
                    _ =>
                        (StatusCodes.Status502BadGateway, "Không thể kết nối đến máy chủ Microservice nội bộ (Bad Gateway). Vui lòng thử lại sau.")
                };

                context.Response.Clear();
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";

                var payload = ApiResponse<object>.Fail(
                    message: message,
                    statusCode: statusCode,
                    errors: [$"Gateway Forwarding Error: {forwarderError.Error}"]
                );

                await JsonSerializer.SerializeAsync(context.Response.Body, payload, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ngoại lệ chưa được xử lý tại API Gateway: {Path}", context.Request.Path);

            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var payload = ApiResponse<object>.Fail(
                    message: "Đã xảy ra lỗi hệ thống nội bộ tại API Gateway.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    errors: [ex.Message]
                );

                await JsonSerializer.SerializeAsync(context.Response.Body, payload, JsonOptions);
            }
        }
    }
}
