namespace server.Domain.Entities;

public class Post
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public DateTime? PublishedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Member Author { get; set; } = null!;
    public ICollection<PostAttachment> Attachments { get; set; } = [];
}
