namespace server.Application.Auth.DTOs;

public record RegisterRequest(
    string AssociationName,
    string Slug,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Phone,
    string CountryCode = "FR",
    string BaseCurrency = "EUR"
);

public record LoginRequest(
    string Email,
    string Password
);

public record RefreshRequest(
    string RefreshToken
);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserInfo User
);

public record UserInfo(
    string Id,
    string Email,
    string FullName,
    string[] Roles,
    string? TenantId,
    string? EntityType,
    string? EntityId,
    string? EmailVerifiedAt
);

public record MeResponse(
    string Id,
    string Email,
    string FullName,
    string[] Roles,
    string? TenantId,
    string? EntityType,
    string? EntityId,
    string? EmailVerifiedAt,
    string[] Permissions
);
