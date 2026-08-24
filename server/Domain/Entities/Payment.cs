namespace server.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MemberId { get; set; }
    public Guid ChargeId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public Guid? PaymentMethodId { get; set; }
    public string? PaymentGateway { get; set; }
    public string? GatewayTransactionId { get; set; }
    public string? GatewayReference { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public DateTime? ReversedAt { get; set; }
    public Guid? ReversedBy { get; set; }
    public string? ReversalReason { get; set; }
    public string? Notes { get; set; }
    public string? ReceiptFileUrl { get; set; }
    public DateOnly PaymentDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Member Member { get; set; } = null!;
    public ContributionCharge Charge { get; set; } = null!;
}
