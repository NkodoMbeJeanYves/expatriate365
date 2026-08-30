namespace server.Domain.Entities;

public class AuditLog
{
    public Guid      Id         { get; set; } = Guid.NewGuid();
    public Guid      UserId     { get; set; }
    public Guid?     TenantId   { get; set; }
    public string    Action     { get; set; } = "";
    public string?   EntityType { get; set; }
    public string?   EntityId   { get; set; }
    public string?   Meta       { get; set; }
    public DateTime  CreatedAt  { get; set; } = DateTime.UtcNow;

    public User?   User   { get; set; }
    public Tenant? Tenant { get; set; }
}
