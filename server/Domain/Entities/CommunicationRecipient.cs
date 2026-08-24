namespace server.Domain.Entities;

public class CommunicationRecipient
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CommunicationId { get; set; }
    public Guid MemberId { get; set; }
    public string Status { get; set; } = "sent";
    public DateTime? ReadAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Communication Communication { get; set; } = null!;
    public Member Member { get; set; } = null!;
}
