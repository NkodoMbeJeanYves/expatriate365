namespace server.Application.SuperAdmin;

public record CreateTenantRequest(
    string AssociationName,
    string Slug,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminPassword,
    string? Phone = null,
    string CountryCode = "MU",
    string BaseCurrency = "MUR"
);

public record TenantSummaryDto(
    string Id,
    string Name,
    string Slug,
    string CountryCode,
    string BaseCurrency,
    bool IsActive,
    string CreatedAt,
    string? UpdatedAt,
    string AdminEmail,
    string AdminFullName,
    int UserCount
);
