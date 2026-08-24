namespace server.Application.Communications.DTOs;

public record CommunicationDto(
    string Id, string TenantId, string Title, string Content,
    string Type, string Channel, string Status, string Audience,
    string? CategoryId, string? TargetMemberId,
    int RecipientCount, int ReadCount,
    string? SentAt, string CreatedAt, string? UpdatedAt);

public record RecipientDto(
    string Id, string MemberId, string MemberName, string MembershipNumber,
    string Status, string? ReadAt, string CreatedAt);

public record CommunicationStatsDto(
    int Total, int Draft, int Sent, int TotalRecipients, int TotalRead);

public record CreateCommunicationRequest(
    string Title, string Content, string Type, string Channel,
    string Audience, string? CategoryId, string? TargetMemberId);

public record UpdateCommunicationRequest(
    string Title, string Content, string Type, string Channel,
    string Audience, string? CategoryId, string? TargetMemberId);
