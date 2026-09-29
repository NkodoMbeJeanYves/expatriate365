namespace server.Application.Governance.DTOs;

public record BoardRoleDto(
    string Id,
    string TenantId,
    string Name,
    string Label,
    bool IsActive,
    string CreatedAt,
    string? UpdatedAt);

public record BoardMemberDto(
    string Id,
    string TenantId,
    string MemberId,
    string MemberName,
    string MembershipNumber,
    string? RoleId,
    string? RoleName,
    string? RoleLabel,
    string StartDate,
    string? EndDate,
    string? Notes,
    string CreatedAt,
    string? UpdatedAt);

public record ResolutionDto(
    string Id, string TenantId, string Title, string Content, string Status,
    string? MeetingId, string? AdoptedAt, int VotesFor, int VotesAgainst, int Abstentions,
    string CreatedAt, string? UpdatedAt);

public record GovernanceStatsDto(
    int TotalBoardMembers, int TotalResolutions, int AdoptedResolutions);

public record CreateBoardRoleRequest(string Name, string Label);
public record UpdateBoardRoleRequest(string Name, string Label, bool IsActive);

public record CreateBoardMemberRequest(
    string MemberId, string? RoleId, string StartDate, string? EndDate, string? Notes);

public record CreateResolutionRequest(
    string Title, string Content, string? MeetingId);

public record AdoptResolutionRequest(
    string AdoptedAt, int VotesFor, int VotesAgainst, int Abstentions);
