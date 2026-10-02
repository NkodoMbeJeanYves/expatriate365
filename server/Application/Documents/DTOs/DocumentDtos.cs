namespace server.Application.Documents.DTOs;

public record DocumentDto(
    string Id, string TenantId, string Title, string? Description,
    string Type, string Category, string FileName, string FileUrl,
    long FileSizeBytes, string MimeType, bool IsPublic, string Visibility,
    string UploadedBy, string UploaderName,
    string CreatedAt, string? UpdatedAt);

public record DocumentStatsDto(int Total, int Public, int Private, List<DocumentCategoryStatDto> ByCategory);

public record DocumentCategoryStatDto(string Category, int Count);

public record CreateDocumentRequest(
    string Title, string? Description, string Type, string Category,
    string FileName, string FileUrl, long FileSizeBytes, string MimeType,
    bool IsPublic, string Visibility = "public");

public record UpdateDocumentRequest(
    string Title, string? Description, string Type, string Category,
    bool IsPublic, string Visibility = "public");
