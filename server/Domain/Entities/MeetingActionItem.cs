namespace server.Domain.Entities;

public class MeetingActionItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MeetingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AssignedToMemberId { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Status { get; set; } = "open";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Meeting Meeting { get; set; } = null!;
    public Member? AssignedTo { get; set; }
}
