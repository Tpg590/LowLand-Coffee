using Auth.API.Entities;

namespace Auth.API.Services.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    (string Token, DateTime ExpiresAt) GenerateRefreshToken();
    int GetTokenExpiryMinutes();
}
