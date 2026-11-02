namespace server.API.Showcases;

/// <summary>Public product or service entry displayed on a community showcase.</summary>
public sealed record ShowcaseOfferResponse(
    Guid Id,
    string Name,
    string Description,
    decimal? Price,
    bool PriceOnRequest,
    string? Details,
    int Position);

/// <summary>Public showcase details with only the contact information chosen for display.</summary>
public sealed record ShowcaseResponse(
    Guid Id,
    string Slug,
    string DisplayName,
    string Title,
    string Description,
    string Category,
    string? ServiceArea,
    string? ContactPhone,
    string? ContactLink,
    bool ContactEnabled,
    string? PhotoUrl,
    string Status,
    string TenantName,
    string TenantSlug,
    string CurrencyCode,
    string CurrencySymbol,
    bool ContactAvailable,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<ShowcaseOfferResponse> Offers);

/// <summary>Member-provided product or service information.</summary>
public sealed record ShowcaseOfferRequest(
    string Name,
    string Description,
    decimal? Price,
    bool PriceOnRequest,
    string? Details);

/// <summary>Complete editable content for the current member's showcase.</summary>
public sealed record SaveShowcaseRequest(
    string DisplayName,
    string Title,
    string Description,
    string Category,
    string? ServiceArea,
    string? ContactPhone,
    string? ContactLink,
    bool ContactEnabled,
    string? PhotoUrl,
    IReadOnlyList<ShowcaseOfferRequest> Offers);

/// <summary>Visitor message delivered privately to the showcase owner's contact email.</summary>
public sealed record ShowcaseContactRequest(
    string Name,
    string Email,
    string Message,
    string? Website);

/// <summary>Anonymous report of a potentially inappropriate showcase.</summary>
public sealed record ShowcaseReportRequest(string Reason, string? Website);

/// <summary>Moderator decision for a pending showcase report.</summary>
public sealed record ResolveShowcaseReportRequest(string Action, string? Note);

/// <summary>Report information shown to community moderators.</summary>
public sealed record ShowcaseReportResponse(
    Guid Id,
    Guid ShowcaseId,
    string ShowcaseTitle,
    string ShowcaseSlug,
    string Reason,
    string Status,
    string? ResolutionNote,
    DateTimeOffset CreatedAt);
