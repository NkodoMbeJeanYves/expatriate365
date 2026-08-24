namespace server.Application.Analytics.DTOs;

public record AnalyticsOverviewDto(
    int TotalMembers, int ActiveMembers, int NewMembersThisMonth,
    decimal TotalCollected, decimal PendingAmount,
    int TotalEvents, int UpcomingEvents,
    int TotalMeetings, int TotalElections);

public record MonthlySeriesPoint(string Month, decimal Value);

public record MembersByStatusDto(string Status, int Count);

public record MemberAnalyticsDto(
    List<MonthlySeriesPoint> MonthlyGrowth,
    List<MembersByStatusDto> ByStatus);

public record FinanceAnalyticsDto(
    List<MonthlySeriesPoint> MonthlyCollected,
    List<MonthlySeriesPoint> MonthlyExpected,
    decimal TotalCollected,
    decimal TotalExpected,
    decimal CollectionRate);

public record EngagementAnalyticsDto(
    decimal MeetingAttendanceRate,
    decimal ElectionParticipationRate,
    int TotalEventRegistrations);
