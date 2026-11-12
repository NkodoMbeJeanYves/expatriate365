namespace server.Domain.Entities;

public class ElectionCandidate
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ElectionId { get; set; }
    public Guid MemberId { get; set; }
    public string? Statement { get; set; }
    public int DisplayOrder { get; set; }
    public string Status { get; set; } = "pending";
    public string? ReviewReason { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Election Election { get; set; } = null!;
    public Member Member { get; set; } = null!;
}
