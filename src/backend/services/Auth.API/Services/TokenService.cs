using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auth.API.Entities;
using Auth.API.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace Auth.API.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public int GetTokenExpiryMinutes()
    {
        if (int.TryParse(_configuration["JwtSettings:ExpiryInMinutes"], out var minutes) && minutes > 0)
        {
            return minutes;
        }

        if (int.TryParse(_configuration["JwtSettings:ExpiryInHours"], out var hours) && hours > 0)
        {
            return hours * 60;
        }

        return 60; // Mặc định 60 phút theo đặc tả PRN232
    }

    public string GenerateAccessToken(User user)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] 
            ?? "LowLandCoffee_Super_Secret_Key_For_PRN232_Assignment_2026_Key";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "LowLandCoffee.Auth";
        var audience = _configuration["JwtSettings:Audience"] ?? "LowLandCoffee.Clients";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role?.Name ?? "Customer"),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiryMinutes = GetTokenExpiryMinutes();
        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public (string Token, DateTime ExpiresAt) GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        // Refresh token có hiệu lực 7 ngày theo đặc tả AUTH-API.md
        var expiresAt = DateTime.UtcNow.AddDays(7);
        return (token, expiresAt);
    }
}
