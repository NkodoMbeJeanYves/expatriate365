namespace server.Application.TransferAssistance;

public static class TransferAssistanceNotificationTypes
{
    public const string Review = "transfer_assistance.review";
    public const string Offer = "transfer_assistance.offer";
    public const string OfferDeclined = "transfer_assistance.offer_declined";
    public const string Assignment = "transfer_assistance.assignment";
    public const string WitnessInvitation = "transfer_assistance.witness_invitation";
    public const string WitnessAccepted = "transfer_assistance.witness_accepted";
    public const string WitnessDeclined = "transfer_assistance.witness_declined";
    public const string Meeting = "transfer_assistance.meeting";
    public const string BeneficiaryConfirmation = "transfer_assistance.beneficiary_confirmation";
    public const string CaseUpdate = "transfer_assistance.case_update";
    public const string Expiration = "transfer_assistance.expiration";
    public const string BeneficiaryCandidate = "transfer_assistance.beneficiary_candidate";
    public const string BeneficiaryDesignated = "transfer_assistance.beneficiary_designated";
    public const string BeneficiaryCandidateDeclined = "transfer_assistance.beneficiary_candidate_declined";

    /// <summary>Channel preference: disabling it stops every email of the module but keeps in-app notifications.</summary>
    public const string EmailChannel = "transfer_assistance.email";

    public static readonly string[] All =
    [
        Review, Offer, OfferDeclined, Assignment, WitnessInvitation, WitnessAccepted, WitnessDeclined,
        Meeting, BeneficiaryConfirmation, CaseUpdate, Expiration, BeneficiaryCandidate,
        BeneficiaryDesignated, BeneficiaryCandidateDeclined, EmailChannel,
    ];
}
