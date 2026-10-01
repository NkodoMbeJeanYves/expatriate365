namespace server.Domain.Entities;

public class PostComment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PostId { get; set; }
    public Guid AuthorMemberId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Post Post { get; set; } = null!;
    public Member Author { get; set; } = null!;
}
