namespace server.Domain.Entities;

public static class TransferAssistanceStatuses
{
    public const string Draft = "draft";
    public const string Submitted = "submitted";
    public const string Matching = "matching";
    public const string WitnessPending = "witness_pending";
    public const string MeetingScheduled = "meeting_scheduled";
    public const string TransferInProgress = "transfer_in_progress";
    public const string TransferSent = "transfer_sent";
    public const string Disputed = "disputed";
    public const string Completed = "completed";
    public const string Expired = "expired";
    public const string Rejected = "rejected";
    public const string Cancelled = "cancelled";
    public const string Suspended = "suspended";
}

public class TransferAssistanceRequest
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequesterMemberId { get; set; }
    public string? OpenRequestKey { get; set; }
    public Guid? BeneficiaryMemberId { get; set; }
    public string BeneficiaryName { get; set; } = string.Empty;
    public string DestinationCountry { get; set; } = string.Empty;
    public decimal AmountMur { get; set; }
    public string ReceiveCurrency { get; set; } = string.Empty;
    public string ReceiveMethod { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string BeneficiaryRelationship { get; set; } = string.Empty;
    public DateOnly AvailableFrom { get; set; }
    public DateOnly AvailableTo { get; set; }
    public string MeetingArea { get; set; } = string.Empty;
    public string Status { get; set; } = TransferAssistanceStatuses.Draft;
    public Guid? HelperMemberId { get; set; }
    public Guid? WitnessMemberId { get; set; }
    public DateTime? MeetingAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool RequesterHandoverConfirmed { get; set; }
    public bool HelperHandoverConfirmed { get; set; }
    public bool WitnessAttested { get; set; }
    public DateTime? HandoverConfirmedAt { get; set; }
    public string? TransferProvider { get; set; }
    public string? TransferEvidenceReference { get; set; }
    public DateTime? TransferSentAt { get; set; }
    public string? BeneficiaryConfirmation { get; set; }
    public DateTime? BeneficiaryConfirmedAt { get; set; }
    public string? DisputeReason { get; set; }
    public Guid? DisputeOpenedBy { get; set; }
    public DateTime? DisputeOpenedAt { get; set; }
    public string? DisputeResolution { get; set; }
    public Guid? DisputeResolvedBy { get; set; }
    public DateTime? DisputeResolvedAt { get; set; }
    public string? SuspendedFromStatus { get; set; }
    public long WorkflowVersion { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewReason { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Member RequesterMember { get; set; } = null!;
    public Member? BeneficiaryMember { get; set; }
    public Member? HelperMember { get; set; }
    public Member? WitnessMember { get; set; }
    public ICollection<TransferAssistanceOffer> Offers { get; set; } = new List<TransferAssistanceOffer>();
    public ICollection<TransferAssistanceWitnessInvitation> WitnessInvitations { get; set; } = new List<TransferAssistanceWitnessInvitation>();
}
