namespace server.Domain.Entities;

public static class TransferAssistanceWitnessInvitationStatuses
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
    public const string Declined = "declined";
    public const string Expired = "expired";
}

public class TransferAssistanceWitnessInvitation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequestId { get; set; }
    public Guid WitnessMemberId { get; set; }
    public Guid InvitedByUserId { get; set; }
    public string Status { get; set; } = TransferAssistanceWitnessInvitationStatuses.Pending;
    public bool ConflictDeclared { get; set; }
    public string? ConflictReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    public TransferAssistanceRequest Request { get; set; } = null!;
    public Member WitnessMember { get; set; } = null!;
}
