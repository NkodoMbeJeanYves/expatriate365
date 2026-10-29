namespace server.Application.TransferAssistance;

public sealed class TransferAssistanceOptions
{
    public bool RequireMfa { get; set; } = true;
    public decimal MinimumRequestAmountMur { get; set; } = 1;
    public decimal MaximumRequestAmountMur { get; set; }
    public int MatchingWindowDays { get; set; } = 14;
    public int InvitationWindowDays { get; set; } = 5;
    public bool EmailNotificationsEnabled { get; set; } = true;
    public int EmailThrottleMinutes { get; set; } = 10;
}
