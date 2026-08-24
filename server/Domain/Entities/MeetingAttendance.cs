namespace server.Domain.Entities;

public class MeetingAttendance
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MeetingId { get; set; }
    public Guid MemberId { get; set; }
    public string Status { get; set; } = "absent";
    public string? ProxyName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Meeting Meeting { get; set; } = null!;
    public Member Member { get; set; } = null!;
}
