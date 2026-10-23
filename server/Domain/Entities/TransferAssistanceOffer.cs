namespace server.Domain.Entities;

public static class TransferAssistanceOfferStatuses
{
    public const string Pending = "pending";
    public const string Selected = "selected";
    public const string Declined = "declined";
    public const string Withdrawn = "withdrawn";
}

public class TransferAssistanceOffer
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequestId { get; set; }
    public Guid HelperMemberId { get; set; }
    public string Availability { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = TransferAssistanceOfferStatuses.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TransferAssistanceRequest Request { get; set; } = null!;
    public Member HelperMember { get; set; } = null!;
}
