namespace server.Domain.Entities;

public class Expense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "other";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime Date { get; set; }
    public string Status { get; set; } = "pending";
    public Guid? ValidatedBy { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public User? Validator { get; set; }
}
