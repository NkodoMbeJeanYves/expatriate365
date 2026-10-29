namespace server.Domain.Entities;

public static class TransferAssistanceVerificationStatuses
{
    public const string NotRequested = "not_requested";
    public const string Pending = "pending";
    public const string Verified = "verified";
    public const string Rejected = "rejected";
}

public class TransferAssistanceProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MemberId { get; set; }
    public bool HelperOptedIn { get; set; }
    public string HelperVerificationStatus { get; set; } = TransferAssistanceVerificationStatuses.NotRequested;
    public Guid? HelperReviewedBy { get; set; }
    public DateTime? HelperReviewedAt { get; set; }
    public string? HelperVerificationReason { get; set; }
    public bool WitnessOptedIn { get; set; }
    public string WitnessVerificationStatus { get; set; } = TransferAssistanceVerificationStatuses.NotRequested;
    public Guid? WitnessReviewedBy { get; set; }
    public DateTime? WitnessReviewedAt { get; set; }
    public string? WitnessVerificationReason { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Member Member { get; set; } = null!;
}
