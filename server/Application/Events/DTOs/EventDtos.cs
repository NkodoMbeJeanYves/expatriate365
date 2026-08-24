namespace server.Application.Events.DTOs;

public record EventDto(
    string Id,
    string TenantId,
    string Title,
    string? Description,
    string Type,
    string Status,
    string? Location,
    string StartDate,
    string EndDate,
    int? MaxCapacity,
    int RegistrationCount,
    int AttendedCount,
    bool IsPublic,
    string CreatedAt,
    string? UpdatedAt
);

public record EventRegistrationDto(
    string Id,
    string EventId,
    string MemberId,
    string MemberName,
    string MembershipNumber,
    string Status,
    string? AttendedAt,
    string CreatedAt
);

public record EventStatsDto(
    int TotalCount,
    int DraftCount,
    int PublishedCount,
    int CompletedCount,
    int CancelledCount,
    int TotalRegistrations,
    int TotalAttended
);

public record CreateEventRequest(
    string Title,
    string? Description,
    string Type,
    string Location,
    string StartDate,
    string EndDate,
    int? MaxCapacity,
    bool IsPublic
);

public record UpdateEventRequest(
    string Title,
    string? Description,
    string Type,
    string? Location,
    string StartDate,
    string EndDate,
    int? MaxCapacity,
    bool IsPublic
);

public record RegisterToEventRequest(
    string MemberId
);

public record MarkAttendanceRequest(
    List<AttendanceEntry> Entries
);

public record AttendanceEntry(
    string RegistrationId,
    string Status
);
