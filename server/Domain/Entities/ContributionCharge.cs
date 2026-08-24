namespace server.Domain.Entities;

public class ContributionCharge
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MemberId { get; set; }
    public Guid ContributionTypeId { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal WaiverAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = "pending";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public decimal TotalDue => BaseAmount + PenaltyAmount - WaiverAmount;
    public decimal Balance => TotalDue - AmountPaid;

    public Member Member { get; set; } = null!;
    public ContributionType ContributionType { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = [];
}
