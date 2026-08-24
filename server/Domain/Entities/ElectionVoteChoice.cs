namespace server.Domain.Entities;

/// <summary>Anonymous ballot choice — no voter link, preserves anonymity.</summary>
public class ElectionVoteChoice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ElectionId { get; set; }
    public Guid CandidateId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ElectionCandidate Candidate { get; set; } = null!;
}
