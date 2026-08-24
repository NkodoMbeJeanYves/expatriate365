namespace server.Domain.Entities;

/// <summary>Records THAT a member voted — not FOR WHOM (anonymity preserved).</summary>
public class ElectionVote
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ElectionId { get; set; }
    public Guid VoterId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Election Election { get; set; } = null!;
}
