namespace server.Domain.Entities;

public class Member
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string MembershipNumber { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public Guid? ChapterId { get; set; }
    public string Status { get; set; } = "pending";
    public DateOnly JoinedDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Address { get; set; }
    public string? Profession { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public User User { get; set; } = null!;
    public MembershipCategory? Category { get; set; }
    public ICollection<ContributionCharge> Charges { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}
