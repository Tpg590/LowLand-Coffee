using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ApiGateway.Models;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Middleware;

/// <summary>
/// Middleware kiểm tra tính hợp lệ và chữ ký (Signature) của Header JWT Bearer Token tại Gateway (PRN232).
/// Hỗ trợ cả Authorization Header thông thường và access_token query param của SignalR WebSocket.
/// Ngăn chặn tấn công Header Spoofing và forward context an toàn xuống Microservices.
/// </summary>
public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtValidationMiddleware> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly TokenValidationParameters _tokenValidationParameters;

    public JwtValidationMiddleware(RequestDelegate next, ILogger<JwtValidationMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;

        var secretKey = configuration["JwtSettings:SecretKey"] 
                        ?? "LowLandCoffee_Super_Secret_Key_For_PRN232_Assignment_2026_Key";
        var issuer = configuration["JwtSettings:Issuer"] ?? "LowLandCoffee.Auth";
        var audience = configuration["JwtSettings:Audience"] ?? "LowLandCoffee.Clients";

        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = !string.IsNullOrEmpty(issuer),
            ValidIssuer = issuer,
            ValidateAudience = !string.IsNullOrEmpty(audience),
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 0. Xóa bỏ triệt để các header nhận diện nội bộ do client ngoài gửi lên
        // nhằm chống tấn công giả mạo danh tính (Header Spoofing / Tampering)
        context.Request.Headers.Remove("X-User-Id");
        context.Request.Headers.Remove("X-User-Role");
        context.Request.Headers.Remove("X-User-Email");

        string? rawToken = null;

        // 1. Kiểm tra Authorization Header
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerValue = authHeader.ToString().Trim();
            if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                rawToken = headerValue["Bearer ".Length..].Trim();
            }
        }
        // 2. Hỗ trợ SignalR Hub WebSocket: Lấy access_token từ Query String nếu có
        else if (context.Request.Query.TryGetValue("access_token", out var queryToken) &&
                 context.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
        {
            rawToken = queryToken.ToString().Trim();
            if (!string.IsNullOrEmpty(rawToken))
            {
                // Đồng bộ sang Header cho downstream microservice nhận dạng
                context.Request.Headers["Authorization"] = $"Bearer {rawToken}";
            }
        }

        // 3. Nếu có token, kiểm tra chữ ký (Signature), tính hợp lệ & thời hạn sống
        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            try
            {
                var principal = _tokenHandler.ValidateToken(rawToken, _tokenValidationParameters, out var validatedToken);

                // Trích xuất Claims từ Token đã được xác thực an toàn
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? principal.FindFirst("sub")?.Value
                             ?? principal.FindFirst("userId")?.Value
                             ?? principal.FindFirst("id")?.Value;

                var role = principal.FindFirst(ClaimTypes.Role)?.Value
                           ?? principal.FindFirst("role")?.Value;

                var email = principal.FindFirst(ClaimTypes.Email)?.Value
                            ?? principal.FindFirst("email")?.Value;

                // Forwarding Headers an toàn xuống Microservices nội bộ
                if (!string.IsNullOrEmpty(userId))
                {
                    context.Request.Headers["X-User-Id"] = userId;
                }

                if (!string.IsNullOrEmpty(role))
                {
                    context.Request.Headers["X-User-Role"] = role;
                }

                if (!string.IsNullOrEmpty(email))
                {
                    context.Request.Headers["X-User-Email"] = email;
                }

                // Cập nhật User vào HttpContext hiện tại
                context.User = principal;
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning("Token đã hết hạn vào lúc {Expires}: {Path}", ex.Expires, context.Request.Path);
                await WriteUnauthorizedResponseAsync(context, "Token xác thực đã hết hạn. Vui lòng đăng nhập lại.");
                return;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Token xác thực không hợp lệ: {Path} - {Message}", context.Request.Path, ex.Message);
                await WriteUnauthorizedResponseAsync(context, "Token xác thực không hợp lệ hoặc chữ ký không đúng.");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi xác thực JWT Token tại Gateway: {Path}", context.Request.Path);
                await WriteUnauthorizedResponseAsync(context, "Không thể xác thực Token gửi lên.");
                return;
            }
        }

        // Cho phép request tiếp tục đi tới YARP Reverse Proxy
        await _next(context);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static async Task WriteUnauthorizedResponseAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var payload = ApiResponse<object>.Fail(message, StatusCodes.Status401Unauthorized);
        await JsonSerializer.SerializeAsync(context.Response.Body, payload, JsonOptions);
    }
}
