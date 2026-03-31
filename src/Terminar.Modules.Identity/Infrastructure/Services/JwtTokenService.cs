using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Terminar.Modules.Identity.Infrastructure.Identity;

namespace Terminar.Modules.Identity.Infrastructure.Services;

public sealed class JwtTokenService(
    UserManager<AppIdentityUser> userManager,
    IConfiguration configuration)
{
    private const string RefreshTokenProvider = "Default";
    private const string RefreshTokenName = "RefreshToken";

    public string GenerateAccessToken(AppIdentityUser user)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured.");
        var issuer = jwtSettings["Issuer"] ?? "terminar";
        var audience = jwtSettings["Audience"] ?? "terminar-api";
        var expiryMinutes = int.TryParse(jwtSettings["ExpiryMinutes"], out var mins) ? mins : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("tenant_id", user.TenantId.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshTokenAsync(AppIdentityUser user)
    {
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        await userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName, refreshToken);
        return refreshToken;
    }

    public async Task<AppIdentityUser?> ValidateRefreshTokenAsync(string userId, string refreshToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
            return null;

        var storedToken = await userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        if (storedToken != refreshToken)
            return null;

        // Rotate: remove old token
        await userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        return user;
    }
}
