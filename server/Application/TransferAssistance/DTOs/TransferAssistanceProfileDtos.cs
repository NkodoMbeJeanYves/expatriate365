namespace server.Application.TransferAssistance.DTOs;

public sealed record UpdateTransferAssistanceProfileRequest(
    bool HelperOptedIn = false, bool WitnessOptedIn = false,
    string? ParticipantRole = null, string? Action = null);

public sealed record TransferAssistanceProfileResponse(
    bool HelperOptedIn,
    string HelperVerificationStatus,
    bool WitnessOptedIn,
    string WitnessVerificationStatus);

public sealed record ReviewTransferAssistanceProfileRequest(string ParticipantRole, string Decision, string? Reason);

/// <summary>Participation rules shown on the information page; mirrors the server-side guards.</summary>
public sealed record TransferAssistanceRulesResponse(
    bool RequireMfa,
    int MatchingWindowDays,
    int InvitationWindowDays,
    decimal MinimumRequestAmount,
    decimal MaximumRequestAmount,
    string AmountCurrency,
    bool RequestsOpen,
    IReadOnlyList<string> VerifierRoles,
    TransferAssistanceMyEligibility MyEligibility);

public sealed record TransferAssistanceMyEligibility(
    bool MemberActive,
    bool EmailVerified,
    bool MfaEnabled,
    bool IsCommunityAdmin,
    bool CanRequestAccreditation);

public sealed record TransferAssistanceProfileReviewItem(
    Guid MemberId,
    string MemberName,
    bool HelperOptedIn,
    string HelperVerificationStatus,
    bool WitnessOptedIn,
    string WitnessVerificationStatus);
