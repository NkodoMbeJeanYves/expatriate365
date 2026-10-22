import { PaginationMeta } from '@shared/models/pagination.model';

export type TransferAssistanceStatus =
  | 'draft'
  | 'submitted'
  | 'matching'
  | 'witness_pending'
  | 'meeting_scheduled'
  | 'transfer_in_progress'
  | 'transfer_sent'
  | 'disputed'
  | 'suspended'
  | 'expired'
  | 'completed'
  | 'rejected'
  | 'cancelled';

export type TransferAssistanceReceiveMethod = 'bank_transfer' | 'mobile_money' | 'orange_money';

export interface TransferAssistanceRequest {
  id: string;
  tenant_id: string;
  requester_member_id: string;
  beneficiary_member_id: string | null;
  beneficiary_name: string;
  destination_country: string;
  amount_mur: number;
  receive_currency: string;
  receive_method: TransferAssistanceReceiveMethod;
  purpose: string;
  beneficiary_relationship: string;
  available_from: string;
  available_to: string;
  meeting_area: string;
  status: TransferAssistanceStatus;
  review_reason?: string;
  helper_member_id?: string;
  witness_member_id?: string;
  meeting_at?: string;
  expires_at?: string;
  requester_handover_confirmed: boolean;
  helper_handover_confirmed: boolean;
  witness_attested: boolean;
  transfer_provider?: string;
  transfer_evidence_reference?: string;
  transfer_sent_at?: string;
  beneficiary_confirmation?: 'full' | 'partial' | 'not_received';
  beneficiary_confirmed_at?: string;
  dispute_reason?: string;
  created_at: string;
  updated_at?: string;
}

export interface PagedTransferAssistanceRequests {
  data: TransferAssistanceRequest[];
  pagination: PaginationMeta;
}

export interface CreateTransferAssistanceRequest {
  beneficiary_member_id: string;
  destination_country: string;
  amount_mur: number;
  receive_currency: string;
  receive_method: TransferAssistanceReceiveMethod;
  purpose: string;
  beneficiary_relationship: string;
  available_from: string;
  available_to: string;
  meeting_area: string;
}

export interface TransferAssistanceBeneficiaryOption {
  member_id: string;
  member_name: string;
}

export interface ReviewTransferAssistanceRequest {
  decision: 'approve' | 'reject' | 'suspend';
  reason?: string;
}

export interface TransferAssistanceAvailableRequest {
  id: string;
  destination_country: string;
  amount_mur: number;
  receive_currency: string;
  receive_method: TransferAssistanceReceiveMethod;
  purpose: string;
  beneficiary_relationship: string;
  available_from: string;
  available_to: string;
  meeting_area: string;
  expires_at: string;
}

export interface TransferAssistanceOffer {
  id: string;
  request_id: string;
  helper_member_id: string;
  helper_name: string;
  availability: string;
  message: string;
  status: 'pending' | 'selected' | 'declined' | 'withdrawn';
  created_at: string;
}

export interface SubmitTransferAssistanceOffer {
  availability: string;
  message: string;
}

export interface TransferAssistanceInvitation {
  invitation_id: string;
  request_id: string;
  destination_country: string;
  amount_mur: number;
  receive_currency: string;
  expires_at: string;
}

export interface TransferAssistanceAssignment {
  request_id: string;
  role: 'helper' | 'witness' | 'beneficiary';
  status: TransferAssistanceStatus;
  destination_country: string;
  amount_mur: number;
  receive_currency: string;
  receive_method: TransferAssistanceReceiveMethod;
  purpose: string;
  available_from: string;
  available_to: string;
  meeting_area: string;
  helper_member_id?: string;
  witness_member_id?: string;
  meeting_at?: string;
  invitation_status?: 'pending' | 'accepted' | 'declined' | 'expired';
  requester_handover_confirmed: boolean;
  helper_handover_confirmed: boolean;
  witness_attested: boolean;
  transfer_provider?: string;
  transfer_evidence_reference?: string;
  transfer_sent_at?: string;
  beneficiary_confirmation?: 'full' | 'partial' | 'not_received';
  dispute_reason?: string;
}

export interface TransferAssistanceReviewCase {
  id: string;
  status: 'disputed' | 'suspended';
  destination_country: string;
  amount_mur: number;
  receive_currency: string;
  beneficiary_name: string;
  review_reason?: string;
  dispute_reason?: string;
  created_at: string;
  updated_at?: string;
}

export interface PagedTransferAssistanceReviewCases {
  data: TransferAssistanceReviewCase[];
  pagination: PaginationMeta;
}

export interface TransferAssistanceEvent {
  event_type: string;
  details?: string;
  created_at: string;
}

export interface TransferAssistanceProfile {
  helper_opted_in: boolean;
  helper_verification_status: 'not_requested' | 'pending' | 'verified' | 'rejected';
  witness_opted_in: boolean;
  witness_verification_status: 'not_requested' | 'pending' | 'verified' | 'rejected';
}

export interface TransferAssistanceMyEligibility {
  member_active: boolean;
  email_verified: boolean;
  mfa_enabled: boolean;
  is_community_admin: boolean;
  can_request_accreditation: boolean;
}

export interface TransferAssistanceRules {
  require_mfa: boolean;
  matching_window_days: number;
  invitation_window_days: number;
  minimum_request_amount: number;
  maximum_request_amount: number;
  amount_currency: string;
  requests_open: boolean;
  verifier_roles: string[];
  my_eligibility: TransferAssistanceMyEligibility;
}

export interface TransferAssistanceProfileReviewItem extends TransferAssistanceProfile {
  member_id: string;
  member_name: string;
}

export interface PagedTransferAssistanceProfiles {
  data: TransferAssistanceProfileReviewItem[];
  pagination: PaginationMeta;
}

export interface UpdateTransferAssistanceProfile {
  participant_role: 'helper' | 'witness';
  action: 'request' | 'cancel';
}

export interface ReviewTransferAssistanceProfile {
  participant_role: 'helper' | 'witness';
  decision: 'verify' | 'reject';
  reason?: string;
}
