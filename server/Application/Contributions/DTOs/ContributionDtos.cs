namespace server.Application.Contributions.DTOs;

public record ContributionTypeDto(
    string Id,
    string TenantId,
    string Name,
    string? Description,
    string Frequency,
    decimal BaseAmount,
    decimal LatePenaltyRate,
    int GracePeriodDays,
    bool IsActive,
    string EffectiveFrom,
    string? EffectiveTo,
    string CreatedAt,
    string? UpdatedAt
);

public record ContributionChargeDto(
    string Id,
    string TenantId,
    string MemberId,
    string? MemberName,
    string? MembershipNumber,
    string ContributionTypeId,
    string? ContributionTypeName,
    string DueDate,
    decimal BaseAmount,
    decimal PenaltyAmount,
    decimal WaiverAmount,
    decimal AmountPaid,
    decimal TotalDue,
    decimal Balance,
    string Status,
    bool IsActive,
    string CreatedAt,
    string? UpdatedAt
);

public record CreateContributionTypeRequest(
    string Name,
    string? Description,
    string Frequency,
    decimal BaseAmount,
    decimal LatePenaltyRate,
    int GracePeriodDays,
    string EffectiveFrom,
    string? EffectiveTo
);

public record UpdateContributionTypeRequest(
    string Name,
    string? Description,
    string Frequency,
    decimal BaseAmount,
    decimal LatePenaltyRate,
    int GracePeriodDays,
    string EffectiveFrom,
    string? EffectiveTo,
    bool IsActive
);

public record CreateContributionChargeRequest(
    string MemberId,
    string ContributionTypeId,
    string DueDate,
    decimal? AmountOverride
);

public record MarkChargePaidRequest(
    decimal AmountPaid,
    string? PaymentReference
);

public record WaiveChargeRequest(
    decimal? WaiverAmount,
    string? Reason
);

public record BulkGenerateRequest(
    string ContributionTypeId,
    string Period,
    string DueDate
);

public record ContributionStatsDto(
    int TotalCharges,
    int PaidCount,
    int PendingCount,
    int OverdueCount,
    int WaivedCount,
    decimal TotalExpected,
    decimal TotalCollected,
    decimal TotalPending
);
