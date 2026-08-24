namespace server.Domain.Entities;

public class MembershipCategory
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal ContributionRate { get; set; } = 100m;
    public bool VotingRights { get; set; } = true;
    public bool WelfareEligible { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<Member> Members { get; set; } = [];
}
