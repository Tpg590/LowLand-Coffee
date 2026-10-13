using System.Collections.Concurrent;
using Auth.API.Services.Interfaces;

namespace Auth.API.Services;

public class InMemoryTokenStore : ITokenStore
{
    private record OtpRecord(string OtpCode, DateTime ExpiresAt, bool IsUsed);

    private readonly ConcurrentDictionary<string, RefreshTokenRecord> _refreshTokens = new();
    private readonly ConcurrentDictionary<string, OtpRecord> _otps = new(StringComparer.OrdinalIgnoreCase);

    public void SaveRefreshToken(string token, Guid userId, DateTime expiresAt)
    {
        _refreshTokens[token] = new RefreshTokenRecord(token, userId, expiresAt, false);
    }

    public RefreshTokenRecord? GetRefreshToken(string token)
    {
        if (_refreshTokens.TryGetValue(token, out var record))
        {
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

    public void SaveOtp(string email, string otpCode, DateTime expiresAt)
    {
        _otps[email.Trim()] = new OtpRecord(otpCode, expiresAt, false);
    }

    public bool ValidateAndConsumeOtp(string email, string otpCode)
    {
        var key = email.Trim();
        if (_otps.TryGetValue(key, out var record))
        {
            if (!record.IsUsed && record.ExpiresAt > DateTime.UtcNow && record.OtpCode == otpCode.Trim())
            {
                _otps[key] = record with { IsUsed = true };
                return true;
            }
        }
        return false;
    }
}
