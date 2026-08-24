namespace server.Domain.Entities;

public class EventRegistration
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid MemberId { get; set; }
    public string Status { get; set; } = "registered";
    public DateTime? AttendedAt { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Event Event { get; set; } = null!;
    public Member Member { get; set; } = null!;
}
