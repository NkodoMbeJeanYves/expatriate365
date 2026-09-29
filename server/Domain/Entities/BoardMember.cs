namespace server.Domain.Entities;

public class BoardMember
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MemberId { get; set; }
    public Guid? RoleId { get; set; }       // nullable: historical records may predate board_roles
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Member Member { get; set; } = null!;
    public BoardRole? BoardRole { get; set; }
}
