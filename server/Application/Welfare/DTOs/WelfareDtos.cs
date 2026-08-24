namespace server.Application.Welfare.DTOs;

public record WelfareRequestDto(
    string Id,
    string TenantId,
    string MemberId,
    string MemberName,
    string MembershipNumber,
    string Type,
    string Description,
    decimal AmountRequested,
    decimal? AmountApproved,
    decimal? AmountPaid,
    string Status,
    string? RejectionReason,
    string? Notes,
    string? ReviewedAt,
    string? PaidAt,
    string CreatedAt,
    string? UpdatedAt
);

public record WelfareStatsDto(
    int TotalCount,
    int PendingCount,
    int ApprovedCount,
    int RejectedCount,
    int PaidCount,
    decimal TotalRequested,
    decimal TotalApproved,
    decimal TotalPaid
);

public record CreateWelfareRequestRequest(
    string MemberId,
    string Type,
    string Description,
    decimal AmountRequested,
    string? Notes
);

public record ApproveWelfareRequestRequest(
    decimal AmountApproved,
    string? Notes
);

public record RejectWelfareRequestRequest(
    string Reason
);
