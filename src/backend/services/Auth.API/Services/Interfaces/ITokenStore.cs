namespace Auth.API.Services.Interfaces;

public record RefreshTokenRecord(string Token, Guid UserId, DateTime ExpiresAt, bool IsRevoked);

public interface ITokenStore
{
    void SaveRefreshToken(string token, Guid userId, DateTime expiresAt);
    RefreshTokenRecord? GetRefreshToken(string token);
    void RevokeRefreshToken(string token);
    void RevokeUserTokens(Guid userId);

    void SaveOtp(string email, string otpCode, DateTime expiresAt);
    bool ValidateAndConsumeOtp(string email, string otpCode);
}
