namespace server.Domain.Entities;

public class Communication
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "announcement";
    public string Channel { get; set; } = "app";
    public string Status { get; set; } = "draft";
    public string Audience { get; set; } = "all";
    public Guid? CategoryId { get; set; }
    public Guid? TargetMemberId { get; set; }
    public DateTime? SentAt { get; set; }
    public int RecipientCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CommunicationRecipient> Recipients { get; set; } = [];
}
