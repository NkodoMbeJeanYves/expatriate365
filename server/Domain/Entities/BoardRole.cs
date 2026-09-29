namespace server.Domain.Entities;

public class BoardRole
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;    // i18n key: "president", "treasurer"
    public string Label { get; set; } = string.Empty;   // display label: "Président(e)"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<BoardMember> BoardMembers { get; set; } = [];
}
