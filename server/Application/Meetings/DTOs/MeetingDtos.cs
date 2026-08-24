namespace server.Application.Meetings.DTOs;

public record MeetingDto(
    string Id, string TenantId, string Title, string Type, string Status,
    string ScheduledAt, string? Location, string? Agenda, int? QuorumRequired,
    int AttendanceCount, int PresentCount, bool HasMinutes,
    string? StartedAt, string? EndedAt, string CreatedAt, string? UpdatedAt);

public record MeetingAttendanceDto(
    string Id, string MeetingId, string MemberId, string MemberName,
    string MembershipNumber, string Status, string? ProxyName, string CreatedAt);

public record MeetingMinuteDto(
    string Id, string MeetingId, string Content, string? Decisions,
    string? AttachmentUrl,
    bool IsApproved, string? ApprovedAt, string CreatedAt, string? UpdatedAt);

public record MeetingStatsDto(
    int Total, int Scheduled, int InProgress, int Completed, int Cancelled);

public record CreateMeetingRequest(
    string Title, string Type, string ScheduledAt,
    string? Location, string? Agenda, int? QuorumRequired);

public record UpdateMeetingRequest(
    string Title, string Type, string ScheduledAt,
    string? Location, string? Agenda, int? QuorumRequired);

public record AttendanceEntry(string MemberId, string Status, string? ProxyName);

public record RecordAttendanceRequest(List<AttendanceEntry> Entries);

public record SaveMinutesRequest(string Content, string? Decisions, string? AttachmentUrl);
