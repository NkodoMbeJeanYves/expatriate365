namespace server.Application.Payments.DTOs;

public record PaymentDto(
    string Id,
    string TenantId,
    string MemberId,
    string MemberName,
    string MembershipNumber,
    string ChargeId,
    string ContributionTypeName,
    string ReceiptNumber,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string? Notes,
    string? ReceiptFileUrl,
    string Status,
    string PaymentDate,
    string? ConfirmedAt,
    string? ReversedAt,
    string? ReversalReason,
    string CreatedAt,
    string? UpdatedAt
);

public record PaymentStatsDto(
    int TotalCount,
    int ConfirmedCount,
    int PendingCount,
    int ReversedCount,
    decimal TotalConfirmed,
    decimal TotalPending
);

public record RecordPaymentRequest(
    string ChargeId,
    decimal Amount,
    string PaymentMethod,
    string PaymentDate,
    string? Notes
);

public record ConfirmPaymentRequest(
    string? Notes
);

public record ReversePaymentRequest(
    string Reason
);
