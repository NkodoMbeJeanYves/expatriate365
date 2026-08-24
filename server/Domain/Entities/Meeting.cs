namespace server.Domain.Entities;

public class Meeting
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "general";
    public string Status { get; set; } = "scheduled";
    public DateTime ScheduledAt { get; set; }
    public string? Location { get; set; }
    public string? Agenda { get; set; }
    public int? QuorumRequired { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<MeetingAttendance> Attendances { get; set; } = [];
    public MeetingMinute? Minute { get; set; }
}
