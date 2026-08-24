namespace server.Domain.Entities;

public class WelfareRequest
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MemberId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal AmountRequested { get; set; }
    public decimal? AmountApproved { get; set; }
    public decimal? AmountPaid { get; set; }
    public string Status { get; set; } = "pending";
    public string? RejectionReason { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? PaidBy { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Member Member { get; set; } = null!;
}
