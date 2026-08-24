namespace server.Domain.Entities;

public class MeetingMinute
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MeetingId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Decisions { get; set; }
    public string? AttachmentUrl { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Meeting Meeting { get; set; } = null!;
}
