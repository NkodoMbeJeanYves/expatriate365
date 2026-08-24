namespace server.Domain.Entities;

/// <summary>Aggregated vote counts per candidate — computed at PublishResults.</summary>
public class ElectionBallot
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ElectionId { get; set; }
    public Guid CandidateId { get; set; }
    public int VoteCount { get; set; }
    public int Rank { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Election Election { get; set; } = null!;
    public ElectionCandidate Candidate { get; set; } = null!;
}
