namespace server.Domain.Entities;

public class Resolution
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string? MeetingId { get; set; }
    public DateOnly? AdoptedAt { get; set; }
    public int VotesFor { get; set; }
    public int VotesAgainst { get; set; }
    public int Abstentions { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
