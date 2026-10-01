namespace server.Domain.Entities;

public class PostReaction
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PostId { get; set; }
    public Guid MemberId { get; set; }
    public string ReactionType { get; set; } = "like";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Post Post { get; set; } = null!;
    public Member Member { get; set; } = null!;
}
