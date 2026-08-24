namespace server.Application.Documents.DTOs;

public record DocumentDto(
    string Id, string TenantId, string Title, string? Description,
    string Type, string Category, string FileName, string FileUrl,
    long FileSizeBytes, string MimeType, bool IsPublic,
    string UploadedBy, string UploaderName,
    string CreatedAt, string? UpdatedAt);

public record DocumentStatsDto(int Total, int Public, int Private);

public record CreateDocumentRequest(
    string Title, string? Description, string Type, string Category,
    string FileName, string FileUrl, long FileSizeBytes, string MimeType, bool IsPublic);

public record UpdateDocumentRequest(
    string Title, string? Description, string Type, string Category, bool IsPublic);
