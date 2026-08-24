namespace server.Application.Elections.DTOs;

public record ElectionDto(
    string Id, string TenantId, string Title, string? Description,
    string Type, string Status, string? StartDate, string? EndDate,
    int MaxChoices, int CandidateCount, int VoteCount,
    string CreatedAt, string? UpdatedAt);

public record ElectionCandidateDto(
    string Id, string ElectionId, string MemberId, string MemberName,
    string MembershipNumber, string? Statement, int DisplayOrder,
    int VoteCount, int Rank);

public record ElectionResultDto(
    string CandidateId, string MemberName, string MembershipNumber,
    int VoteCount, int Rank, double Percentage);

public record ElectionStatsDto(
    int Total, int Draft, int Open, int Closed, int ResultsPublished);

public record CreateElectionRequest(
    string Title, string? Description, string Type,
    string? StartDate, string? EndDate, int MaxChoices);

public record UpdateElectionRequest(
    string Title, string? Description, string Type,
    string? StartDate, string? EndDate, int MaxChoices);

public record AddCandidateRequest(string MemberId, string? Statement, int DisplayOrder);

public record CastVoteRequest(List<string> CandidateIds);
