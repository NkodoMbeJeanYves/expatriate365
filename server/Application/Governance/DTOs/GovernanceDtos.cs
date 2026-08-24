namespace server.Application.Governance.DTOs;

public record BoardMemberDto(
    string Id, string TenantId, string MemberId, string MemberName, string MembershipNumber,
    string Role, string StartDate, string? EndDate, string? Notes,
    string CreatedAt, string? UpdatedAt);

public record ResolutionDto(
    string Id, string TenantId, string Title, string Content, string Status,
    string? MeetingId, string? AdoptedAt, int VotesFor, int VotesAgainst, int Abstentions,
    string CreatedAt, string? UpdatedAt);

public record GovernanceStatsDto(
    int TotalBoardMembers, int TotalResolutions, int AdoptedResolutions);

public record CreateBoardMemberRequest(
    string MemberId, string Role, string StartDate, string? EndDate, string? Notes);

public record UpdateBoardMemberRequest(
    string Role, string StartDate, string? EndDate, string? Notes);

public record CreateResolutionRequest(
    string Title, string Content, string? MeetingId);

public record AdoptResolutionRequest(
    string AdoptedAt, int VotesFor, int VotesAgainst, int Abstentions);
