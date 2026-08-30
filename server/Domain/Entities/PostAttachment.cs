namespace server.Domain.Entities;

public class PostAttachment
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string AttachmentType { get; set; } = "document";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Post Post { get; set; } = null!;
}
