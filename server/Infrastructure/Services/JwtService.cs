using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using server.Application.Auth.DTOs;
using server.Domain.Entities;

namespace server.Infrastructure.Services;

public class JwtService(IConfiguration config)
{
    private readonly string _secret = config["Jwt:SecretKey"]
        ?? throw new InvalidOperationException("Jwt:SecretKey not configured");
    private readonly string _issuer = config["Jwt:Issuer"] ?? "Expatriate365";
    private readonly string _audience = config["Jwt:Audience"] ?? "Expatriate365";
    private readonly int _accessExpiry = int.Parse(config["Jwt:AccessTokenExpiryMinutes"] ?? "15");
    private readonly int _refreshExpiry = int.Parse(config["Jwt:RefreshTokenExpiryDays"] ?? "30");

    public string GenerateAccessToken(
        User user,
        string[]? permissions = null,
        string? entityType = null,
        string? entityId = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var permissionsJson = System.Text.Json.JsonSerializer.Serialize(permissions ?? []);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("full_name", user.FullName),
            new("role", user.Role),
            new("permissions", permissionsJson),
        };
        if (user.TenantId.HasValue)
            claims.Add(new("tenant_id", user.TenantId.Value.ToString()));
        if (entityType is not null)
            claims.Add(new("entity_type", entityType));
        if (entityId is not null)
            claims.Add(new("entity_id", entityId));

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessExpiry),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string plain, string hash) GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var plain = Convert.ToBase64String(bytes);
        var hash = HashToken(plain);
        return (plain, hash);
    }

    public DateTime RefreshTokenExpiry() => DateTime.UtcNow.AddDays(_refreshExpiry);

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public TokenValidationParameters GetValidationParameters() => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
        ValidateIssuer = true,
        ValidIssuer = _issuer,
        ValidateAudience = true,
        ValidAudience = _audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
    };

    public UserInfo ToUserInfo(User user, string? entityType = null, string? entityId = null) => new(
        user.Id.ToString(),
        user.Email,
        user.FullName,
        [user.Role],
        user.TenantId?.ToString(),
        entityType,
        entityId,
        user.EmailVerifiedAt?.ToString("O")
    );

    public int AccessExpirySeconds => _accessExpiry * 60;
}
