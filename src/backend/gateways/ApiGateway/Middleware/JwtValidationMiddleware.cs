using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ApiGateway.Models;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Middleware;

/// <summary>
/// Middleware kiểm tra tính hợp lệ, chữ ký (Signature) và Danh sách đen (Blacklist) của Header JWT Bearer Token tại Gateway (PRN232).
/// Hỗ trợ cả Authorization Header thông thường và access_token query param của SignalR WebSocket.
/// Ngăn chặn tấn công Header Spoofing và forward context an toàn xuống Microservices.
/// </summary>
public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtValidationMiddleware> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly string _authServiceBaseUrl;
    private static readonly HttpClient SharedHttpClient = new() { Timeout = TimeSpan.FromSeconds(1.5) };
    private static readonly ConcurrentDictionary<string, DateTime> BlacklistedTokens = new();

    public JwtValidationMiddleware(RequestDelegate next, ILogger<JwtValidationMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;

        var secretKey = configuration["JwtSettings:SecretKey"] 
                        ?? "LowLandCoffee_Super_Secret_Key_For_PRN232_Assignment_2026_Key";
        var issuer = configuration["JwtSettings:Issuer"] ?? "LowLandCoffee.Auth";
        var audience = configuration["JwtSettings:Audience"] ?? "LowLandCoffee.Clients";

        _authServiceBaseUrl = configuration["ReverseProxy:Clusters:auth-cluster:Destinations:destination1:Address"]?.TrimEnd('/')
                              ?? "http://localhost:5001";

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

        string? tokenJti = null;
        DateTime tokenExpiresAt = DateTime.UtcNow.AddHours(1);

        // 3. Nếu có token, kiểm tra Blacklist, chữ ký (Signature), tính hợp lệ & thời hạn sống
        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            if (IsBlacklistedLocal(rawToken))
            {
                _logger.LogWarning("Từ chối Token nằm trong danh sách đen (Blacklist) tại Gateway: {Path}", context.Request.Path);
                await WriteUnauthorizedResponseAsync(context, "Token đã nằm trong danh sách đen (đã đăng xuất). Vui lòng đăng nhập lại.");
                return;
            }

            try
            {
                var principal = _tokenHandler.ValidateToken(rawToken, _tokenValidationParameters, out var validatedToken);
                if (validatedToken.ValidTo > DateTime.UtcNow)
                {
                    tokenExpiresAt = validatedToken.ValidTo;
                }

                tokenJti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (!string.IsNullOrWhiteSpace(tokenJti) && IsBlacklistedLocal(tokenJti))
                {
                    BlacklistLocal(rawToken, tokenExpiresAt);
                    _logger.LogWarning("Từ chối JTI nằm trong danh sách đen (Blacklist) tại Gateway: {Path}", context.Request.Path);
                    await WriteUnauthorizedResponseAsync(context, "Token đã nằm trong danh sách đen (đã đăng xuất). Vui lòng đăng nhập lại.");
                    return;
                }

                // Kiểm tra đồng bộ Blacklist với Auth.API phòng trường hợp client gọi trực tiếp vào Auth.API
                if (!context.Request.Path.StartsWithSegments("/api/auth/blacklist", StringComparison.OrdinalIgnoreCase) &&
                    await CheckRemoteBlacklistAsync(rawToken, tokenJti))
                {
                    BlacklistLocal(rawToken, tokenExpiresAt);
                    if (!string.IsNullOrWhiteSpace(tokenJti))
                    {
                        BlacklistLocal(tokenJti, tokenExpiresAt);
                    }
                    _logger.LogWarning("Từ chối Token bị thu hồi từ Auth.API Blacklist: {Path}", context.Request.Path);
                    await WriteUnauthorizedResponseAsync(context, "Token đã nằm trong danh sách đen (đã đăng xuất). Vui lòng đăng nhập lại.");
                    return;
                }

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

        // 4. Nếu là request Đăng xuất (/api/auth/logout) thành công, lập tức đưa Token vào danh sách đen tại Gateway
        if (!string.IsNullOrWhiteSpace(rawToken) &&
            HttpMethods.IsPost(context.Request.Method) &&
            context.Request.Path.StartsWithSegments("/api/auth/logout", StringComparison.OrdinalIgnoreCase) &&
            context.Response.StatusCode < 400)
        {
            BlacklistLocal(rawToken, tokenExpiresAt);
            if (!string.IsNullOrWhiteSpace(tokenJti))
            {
                BlacklistLocal(tokenJti, tokenExpiresAt);
            }
            _logger.LogInformation("Đã đưa Access Token vào danh sách đen (Blacklist) sau khi đăng xuất.");
        }
    }

    private static void BlacklistLocal(string key, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var kvp in BlacklistedTokens)
        {
            if (kvp.Value <= now)
            {
                BlacklistedTokens.TryRemove(kvp.Key, out _);
            }
        }

        BlacklistedTokens[key.Trim()] = expiresAt > now ? expiresAt : now.AddHours(1);
    }

    private static bool IsBlacklistedLocal(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var trimmed = key.Trim();
        if (BlacklistedTokens.TryGetValue(trimmed, out var expiresAt))
        {
            if (expiresAt > DateTime.UtcNow)
            {
                return true;
            }

            BlacklistedTokens.TryRemove(trimmed, out _);
        }

        return false;
    }

    private async Task<bool> CheckRemoteBlacklistAsync(string rawToken, string? jti)
    {
        try
        {
            var query = !string.IsNullOrWhiteSpace(jti)
                ? $"jti={Uri.EscapeDataString(jti)}"
                : $"token={Uri.EscapeDataString(rawToken)}";
            using var response = await SharedHttpClient.GetAsync($"{_authServiceBaseUrl}/api/auth/blacklist/check?{query}");
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            if (doc.RootElement.TryGetProperty("isBlacklisted", out var prop) && prop.ValueKind == JsonValueKind.True)
            {
                return true;
            }
        }
        catch
        {
            // Fallback sang bộ nhớ cục bộ của Gateway nếu kết nối tạm thời gián đoạn
        }

        return false;
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
