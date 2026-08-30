namespace server.Application.Audit.DTOs;

public record AuditLogDto(
    string   Id,
    string?  TenantId,
    string?  TenantName,
    string   UserId,
    string   UserName,
    string   Action,
    string?  EntityType,
    string?  EntityId,
    string?  Meta,
    string   CreatedAt);

public record TenantStatsDto(
    string  Id,
    string  Name,
    string  Slug,
    int     MemberCount,
    int     PostsPublished,
    int     PostsDraft,
    int     PostsRejected,
    string? LastActivity);

public record AnomalyDto(
    string  Type,
    string  Description,
    string  Severity,
    string? TenantId,
    string? TenantName,
    string? EntityId);
