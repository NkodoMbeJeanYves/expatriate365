namespace server.Domain.Entities;

public class Election
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "custom";
    public string Status { get; set; } = "draft";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int MaxChoices { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ElectionCandidate> Candidates { get; set; } = [];
    public ICollection<ElectionVote> Votes { get; set; } = [];
    public ICollection<ElectionBallot> Ballots { get; set; } = [];
}
