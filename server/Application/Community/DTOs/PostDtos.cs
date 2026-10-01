namespace server.Application.Community.DTOs;

public record PostAttachmentDto(
    string Id, string PostId, string FileUrl, string FileName,
    string MimeType, long FileSizeBytes, string AttachmentType, string CreatedAt);

public record PostDto(
    string Id, string TenantId, string AuthorId, string AuthorName,
    string Title, string Content, string Status,
    string? PublishedAt, string CreatedAt, string? UpdatedAt,
    IEnumerable<PostAttachmentDto> Attachments);

public record PostSummaryDto(
    string Id, string AuthorId, string AuthorName,
    string Title, string ContentPreview, string Status,
    string? PublishedAt, string CreatedAt, int AttachmentCount);

public record CreatePostRequest(string Title, string Content);

public record UpdatePostRequest(string Title, string Content);

public record AddAttachmentRequest(
    string FileUrl, string FileName, string MimeType,
    long FileSizeBytes, string AttachmentType);

public record PostCommentDto(
    string Id, string PostId, string AuthorMemberId, string AuthorName,
    string Content, string CreatedAt, string? UpdatedAt);

public record PostReactionCountDto(string ReactionType, int Count, bool UserReacted);

public record AddCommentRequest(string Content);

public record ToggleReactionRequest(string ReactionType);
