namespace server.Domain.Entities;

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;       // technical key e.g. "treasurer"
    public string Label { get; set; } = string.Empty;      // display name e.g. "Trésorier"
    public string? Description { get; set; }
    public string Permissions { get; set; } = "[]";        // JSON array stored as string, for future RBAC
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
