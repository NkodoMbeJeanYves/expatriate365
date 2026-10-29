namespace server.Domain.Entities;

public static class TransferAssistanceBeneficiaryCandidateStatuses
{
    public const string Pending = "pending";
    public const string Selected = "selected";
    public const string Declined = "declined";
    public const string Withdrawn = "withdrawn";
}

public class TransferAssistanceBeneficiaryCandidate
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequestId { get; set; }
    public Guid MemberId { get; set; }
    public string? Message { get; set; }
    public string Status { get; set; } = TransferAssistanceBeneficiaryCandidateStatuses.Pending;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public TransferAssistanceRequest Request { get; set; } = null!;
    public Member Member { get; set; } = null!;
}
