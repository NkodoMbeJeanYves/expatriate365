namespace server.Domain.Entities;

public class ContributionType
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Frequency { get; set; } = "monthly";
    public decimal BaseAmount { get; set; }
    public decimal LatePenaltyRate { get; set; }
    public int GracePeriodDays { get; set; } = 15;
    public bool IsActive { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<ContributionCharge> Charges { get; set; } = [];
}
