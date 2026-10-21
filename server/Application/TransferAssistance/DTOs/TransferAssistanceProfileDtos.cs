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

public sealed record TransferAssistanceProfileReviewItem(
    Guid MemberId,
    string MemberName,
    bool HelperOptedIn,
    string HelperVerificationStatus,
    bool WitnessOptedIn,
    string WitnessVerificationStatus);
