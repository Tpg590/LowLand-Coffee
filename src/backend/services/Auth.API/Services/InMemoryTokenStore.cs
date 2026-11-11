using System.Collections.Concurrent;
using Auth.API.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Auth.API.Services;

public class InMemoryTokenStore : ITokenStore, IDisposable
{
    private record OtpRecord(string OtpCode, DateTime ExpiresAt, bool IsUsed);

    private readonly ConcurrentDictionary<string, RefreshTokenRecord> _refreshTokens = new();
    private readonly ConcurrentDictionary<string, DateTime> _blacklistedAccessTokens = new();
    private readonly ConcurrentDictionary<string, OtpRecord> _otps = new(StringComparer.OrdinalIgnoreCase);
    private readonly Timer? _cleanupTimer;
    private readonly ILogger<InMemoryTokenStore>? _logger;

    public InMemoryTokenStore(ILogger<InMemoryTokenStore>? logger = null)
    {
        _logger = logger;
        // Tự động quét và dọn dẹp các token/OTP hết hạn mỗi 10 phút (lần đầu kích hoạt sau 1 phút)
        _cleanupTimer = new Timer(_ => CleanupExpiredTokens(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10));
    }

    public void SaveRefreshToken(string token, Guid userId, DateTime expiresAt)
    {
        _refreshTokens[token] = new RefreshTokenRecord(token, userId, expiresAt, false);
    }

    public RefreshTokenRecord? GetRefreshToken(string token)
    {
        if (_refreshTokens.TryGetValue(token, out var record))
        {
            // Tự động xóa ngay lập tức nếu Refresh Token này đã hết hạn
            if (record.ExpiresAt <= DateTime.UtcNow)
            {
                _refreshTokens.TryRemove(token, out _);
                return null;
            }
            return record;
        }
        return null;
    }

    public void RevokeRefreshToken(string token)
    {
        if (_refreshTokens.TryGetValue(token, out var record))
        {
            _refreshTokens[token] = record with { IsRevoked = true };
        }
    }

    public void RevokeUserTokens(Guid userId)
    {
        foreach (var kvp in _refreshTokens)
        {
            if (kvp.Value.UserId == userId && !kvp.Value.IsRevoked)
            {
                _refreshTokens[kvp.Key] = kvp.Value with { IsRevoked = true };
            }
        }
    }

    public void BlacklistAccessToken(string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var now = DateTime.UtcNow;
        _blacklistedAccessTokens[token.Trim()] = expiresAt > now ? expiresAt : now.AddHours(1);
    }

    public bool IsAccessTokenBlacklisted(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var key = token.Trim();
        if (_blacklistedAccessTokens.TryGetValue(key, out var expiresAt))
        {
            if (expiresAt > DateTime.UtcNow)
            {
                return true;
            }

            // Tự động xóa ngay lập tức nếu Access Token trong Blacklist đã hết hạn
            _blacklistedAccessTokens.TryRemove(key, out _);
        }

        return false;
    }

    public void SaveOtp(string email, string otpCode, DateTime expiresAt)
    {
        _otps[email.Trim()] = new OtpRecord(otpCode, expiresAt, false);
    }

    public bool ValidateAndConsumeOtp(string email, string otpCode)
    {
        var key = email.Trim();
        if (_otps.TryGetValue(key, out var record))
        {
            if (record.ExpiresAt <= DateTime.UtcNow)
            {
                // Tự động xóa OTP nếu đã quá hạn xác thực
                _otps.TryRemove(key, out _);
                return false;
            }

            if (!record.IsUsed && record.OtpCode == otpCode.Trim())
            {
                // Tiêu thụ OTP thành công: xóa ngay để giải phóng RAM và chống tái sử dụng
                _otps.TryRemove(key, out _);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Quét và giải phóng toàn bộ các Refresh Token, Access Token Blacklist, và OTP đã hết hạn khỏi bộ nhớ RAM.
    /// </summary>
    public void CleanupExpiredTokens()
    {
        var now = DateTime.UtcNow;
        int removedRefresh = 0;
        int removedBlacklist = 0;
        int removedOtp = 0;

        // 1. Quét và giải phóng Refresh Token hết hạn hoặc token đã bị thu hồi (revoked) quá hạn
        foreach (var kvp in _refreshTokens)
        {
            if (kvp.Value.ExpiresAt <= now)
            {
                if (_refreshTokens.TryRemove(kvp.Key, out _))
                {
                    removedRefresh++;
                }
            }
        }

        // 2. Quét và giải phóng Blacklist Access Token đã hết hạn
        foreach (var kvp in _blacklistedAccessTokens)
        {
            if (kvp.Value <= now)
            {
                if (_blacklistedAccessTokens.TryRemove(kvp.Key, out _))
                {
                    removedBlacklist++;
                }
            }
        }

        // 3. Quét và giải phóng OTP đã hết hạn hoặc đã sử dụng
        foreach (var kvp in _otps)
        {
            if (kvp.Value.ExpiresAt <= now || kvp.Value.IsUsed)
            {
                if (_otps.TryRemove(kvp.Key, out _))
                {
                    removedOtp++;
                }
            }
        }

        if (removedRefresh > 0 || removedBlacklist > 0 || removedOtp > 0)
        {
            _logger?.LogInformation(
                "[TokenStore] Tự động giải phóng RAM: {RefreshCount} Refresh Token, {BlacklistCount} Blacklisted Token, {OtpCount} OTP hết hạn.",
                removedRefresh, removedBlacklist, removedOtp);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}
