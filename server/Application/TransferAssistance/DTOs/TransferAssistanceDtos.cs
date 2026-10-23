namespace server.Application.TransferAssistance.DTOs;

public sealed record CreateTransferAssistanceRequest(
    Guid? BeneficiaryMemberId,
    string DestinationCountry,
    decimal AmountMur,
    string ReceiveCurrency,
    string ReceiveMethod,
    string Purpose,
    string BeneficiaryRelationship,
    DateOnly AvailableFrom,
    DateOnly AvailableTo,
    string MeetingArea);

public sealed record ReviewTransferAssistanceRequest(string Decision, string? Reason);

public sealed record TransferAssistanceRequestResponse(
    Guid Id,
    Guid TenantId,
    Guid RequesterMemberId,
    Guid? BeneficiaryMemberId,
    string BeneficiaryName,
    string DestinationCountry,
    decimal AmountMur,
    string ReceiveCurrency,
    string ReceiveMethod,
    string Purpose,
    string BeneficiaryRelationship,
    DateOnly AvailableFrom,
    DateOnly AvailableTo,
    string MeetingArea,
    string Status,
    string? ReviewReason,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid? HelperMemberId = null,
    Guid? WitnessMemberId = null,
    DateTime? MeetingAt = null,
    DateTime? ExpiresAt = null,
    bool RequesterHandoverConfirmed = false,
    bool HelperHandoverConfirmed = false,
    bool WitnessAttested = false,
    string? TransferProvider = null,
    string? TransferEvidenceReference = null,
    DateTime? TransferSentAt = null,
    string? BeneficiaryConfirmation = null,
    DateTime? BeneficiaryConfirmedAt = null,
    string? DisputeReason = null,
    bool BeneficiaryPending = false,
    int BeneficiaryCandidateCount = 0);

public sealed record TransferAssistanceBeneficiaryOption(Guid MemberId, string MemberName);

public sealed record TransferAssistanceAvailableRequestResponse(
    Guid Id,
    string DestinationCountry,
    decimal AmountMur,
    string ReceiveCurrency,
    string ReceiveMethod,
    string Purpose,
    string BeneficiaryRelationship,
    DateOnly AvailableFrom,
    DateOnly AvailableTo,
    string MeetingArea,
    DateTime ExpiresAt);

public sealed record SubmitTransferAssistanceOfferRequest(string Availability, string Message);

public sealed record TransferAssistanceOfferResponse(
    Guid Id,
    Guid RequestId,
    Guid HelperMemberId,
    string HelperName,
    string Availability,
    string Message,
    string Status,
    DateTime CreatedAt);

public sealed record InviteTransferAssistanceWitnessRequest(Guid WitnessMemberId);

public sealed record RespondTransferAssistanceWitnessInvitationRequest(
    bool Accept,
    bool ConflictDeclared,
    string? ConflictReason);

public sealed record TransferAssistanceWitnessInvitationResponse(
    Guid Id,
    Guid RequestId,
    Guid WitnessMemberId,
    string WitnessName,
    string Status,
    bool ConflictDeclared,
    string? ConflictReason,
    DateTime CreatedAt);

public sealed record ScheduleTransferAssistanceMeetingRequest(DateTime MeetingAt);

public sealed record TransferAssistanceHandoverConfirmationRequest(bool ConfirmPresenceAndHandover);

public sealed record SubmitTransferAssistanceEvidenceRequest(string Provider, string Reference);

public sealed record ConfirmTransferAssistanceReceiptRequest(string Outcome, string? Reason);

public sealed record OpenTransferAssistanceDisputeRequest(string Reason);

public sealed record ResolveTransferAssistanceDisputeRequest(string Resolution, string Reason);

public sealed record SelectTransferAssistanceOfferRequest(Guid OfferId);

public sealed record AttestTransferAssistanceWitnessRequest(bool Attested);

public sealed record TransferAssistanceMyInvitationResponse(
    Guid InvitationId,
    Guid RequestId,
    string DestinationCountry,
    decimal AmountMur,
    string ReceiveCurrency,
    DateTime ExpiresAt);

public sealed record TransferAssistanceReviewCaseResponse(
    Guid Id,
    string Status,
    string DestinationCountry,
    decimal AmountMur,
    string ReceiveCurrency,
    string BeneficiaryName,
    string? ReviewReason,
    string? DisputeReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record TransferAssistanceAssignmentResponse(
    Guid RequestId,
    string Role,
    string Status,
    string DestinationCountry,
    decimal AmountMur,
    string ReceiveCurrency,
    string ReceiveMethod,
    string Purpose,
    DateOnly AvailableFrom,
    DateOnly AvailableTo,
    string MeetingArea,
    Guid? HelperMemberId,
    Guid? WitnessMemberId,
    DateTime? MeetingAt,
    string? InvitationStatus,
    bool RequesterHandoverConfirmed,
    bool HelperHandoverConfirmed,
    bool WitnessAttested,
    string? TransferProvider,
    string? TransferEvidenceReference,
    DateTime? TransferSentAt,
    string? BeneficiaryConfirmation,
    string? DisputeReason);

public sealed record TransferAssistanceEventResponse(
    string EventType,
    string? Details,
    DateTime CreatedAt);

public sealed record ApplyTransferAssistanceBeneficiaryRequest(string? Message);

public sealed record DesignateTransferAssistanceBeneficiaryRequest(
    Guid? CandidateId,
    Guid? MemberId,
    string? BeneficiaryRelationship);

public sealed record TransferAssistanceBeneficiaryCandidateResponse(
    Guid Id,
    Guid RequestId,
    Guid MemberId,
    string MemberName,
    string? Message,
    string Status,
    DateTime CreatedAt);

public sealed record TransferAssistanceBeneficiaryOpenRequestResponse(
    Guid Id,
    string DestinationCountry,
    decimal AmountMur,
    string ReceiveCurrency,
    string ReceiveMethod,
    string Purpose,
    DateOnly AvailableFrom,
    DateOnly AvailableTo,
    DateTime? ExpiresAt,
    string? MyCandidateStatus,
    Guid? MyCandidateId);

public sealed record TransferAssistanceSyncStateResponse(
    string Version,
    IReadOnlyDictionary<string, string> Sections,
    DateTime ServerTime);
