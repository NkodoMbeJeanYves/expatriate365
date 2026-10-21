namespace server.Domain.Entities;

public class TransferAssistanceEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequestId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TransferAssistanceRequest Request { get; set; } = null!;
}
