import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import {
  CreateTransferAssistanceRequest,
  PagedTransferAssistanceRequests,
  PagedTransferAssistanceProfiles,
  ReviewTransferAssistanceProfile,
  ReviewTransferAssistanceRequest,
  TransferAssistanceBeneficiaryOption,
  TransferAssistanceAvailableRequest,
  TransferAssistanceOffer,
  SubmitTransferAssistanceOffer,
  TransferAssistanceInvitation,
  TransferAssistanceAssignment,
  TransferAssistanceReviewCase,
  PagedTransferAssistanceReviewCases,
  TransferAssistanceEvent,
  TransferAssistanceProfile,
  TransferAssistanceRequest,
  TransferAssistanceRules,
  UpdateTransferAssistanceProfile,
} from '@models/transfer-assistance.model';

@Injectable({ providedIn: 'root' })
export class TransferAssistanceApiService {
  private readonly http = inject(HttpClient);
  private readonly requestsUrl = `${environment.apiUrl}/api/v1/transfer-requests`;
  private readonly reviewsUrl = `${environment.apiUrl}/api/v1/admin/transfer-assistance-requests`;
  private readonly profileUrl = `${environment.apiUrl}/api/v1/transfer-assistance/profile`;
  private readonly profileReviewsUrl = `${environment.apiUrl}/api/v1/admin/transfer-assistance-profiles`;
  private readonly adminCasesUrl = `${environment.apiUrl}/api/v1/admin/transfer-assistance-requests`;
  private readonly rulesUrl = `${environment.apiUrl}/api/v1/transfer-assistance/rules`;

  getRules(): Observable<TransferAssistanceRules> {
    return this.http.get<TransferAssistanceRules>(this.rulesUrl);
  }

  getProfile(): Observable<TransferAssistanceProfile> {
    return this.http.get<TransferAssistanceProfile>(`${this.profileUrl}/me`);
  }

  updateProfile(request: UpdateTransferAssistanceProfile): Observable<TransferAssistanceProfile> {
    return this.http.put<TransferAssistanceProfile>(`${this.profileUrl}/me`, request);
  }

  getProfileReviewQueue(page = 1, limit = 20): Observable<PagedTransferAssistanceProfiles> {
    const params = new HttpParams().set('page', page).set('limit', limit);
    return this.http.get<PagedTransferAssistanceProfiles>(`${this.profileReviewsUrl}/review-queue`, { params });
  }

  reviewProfile(memberId: string, request: ReviewTransferAssistanceProfile): Observable<TransferAssistanceProfile> {
    return this.http.post<TransferAssistanceProfile>(`${this.profileReviewsUrl}/${memberId}/review`, request);
  }

  getMine(page = 1, limit = 20): Observable<PagedTransferAssistanceRequests> {
    const params = new HttpParams().set('page', page).set('limit', limit);
    return this.http.get<PagedTransferAssistanceRequests>(`${this.requestsUrl}/me`, { params });
  }

  searchBeneficiaries(query: string): Observable<TransferAssistanceBeneficiaryOption[]> {
    const params = new HttpParams().set('query', query);
    return this.http.get<TransferAssistanceBeneficiaryOption[]>(`${this.requestsUrl}/beneficiaries`, { params });
  }

  searchWitnesses(query: string): Observable<TransferAssistanceBeneficiaryOption[]> {
    const params = new HttpParams().set('query', query);
    return this.http.get<TransferAssistanceBeneficiaryOption[]>(`${this.requestsUrl}/witnesses`, { params });
  }

  getAvailableRequests(): Observable<TransferAssistanceAvailableRequest[]> {
    return this.http.get<TransferAssistanceAvailableRequest[]>(`${this.requestsUrl}/available`);
  }

  getAssignments(): Observable<TransferAssistanceAssignment[]> {
    return this.http.get<TransferAssistanceAssignment[]>(`${this.requestsUrl}/assignments`);
  }

  getMyWitnessInvitations(): Observable<TransferAssistanceInvitation[]> {
    return this.http.get<TransferAssistanceInvitation[]>(`${this.requestsUrl}/witness-invitations/me`);
  }

  getReviewQueue(page = 1, limit = 20): Observable<PagedTransferAssistanceRequests> {
    const params = new HttpParams().set('page', page).set('limit', limit);
    return this.http.get<PagedTransferAssistanceRequests>(`${this.reviewsUrl}/review-queue`, { params });
  }

  create(request: CreateTransferAssistanceRequest): Observable<TransferAssistanceRequest> {
    return this.http.post<TransferAssistanceRequest>(this.requestsUrl, request);
  }

  updateDraft(id: string, request: CreateTransferAssistanceRequest): Observable<TransferAssistanceRequest> {
    return this.http.put<TransferAssistanceRequest>(`${this.requestsUrl}/${id}`, request);
  }

  submit(id: string): Observable<TransferAssistanceRequest> {
    return this.http.post<TransferAssistanceRequest>(`${this.requestsUrl}/${id}/submit`, {});
  }

  cancel(id: string): Observable<TransferAssistanceRequest> {
    return this.http.post<TransferAssistanceRequest>(`${this.requestsUrl}/${id}/cancel`, {});
  }

  review(id: string, request: ReviewTransferAssistanceRequest): Observable<TransferAssistanceRequest> {
    return this.http.post<TransferAssistanceRequest>(`${this.reviewsUrl}/${id}/review`, request);
  }

  getOffers(id: string): Observable<TransferAssistanceOffer[]> {
    return this.http.get<TransferAssistanceOffer[]>(`${this.requestsUrl}/${id}/offers`);
  }

  submitOffer(id: string, request: SubmitTransferAssistanceOffer): Observable<TransferAssistanceOffer> {
    return this.http.post<TransferAssistanceOffer>(`${this.requestsUrl}/${id}/offers`, request);
  }

  selectOffer(id: string, offerId: string): Observable<TransferAssistanceRequest> {
    return this.http.post<TransferAssistanceRequest>(`${this.requestsUrl}/${id}/select-offer`, { offer_id: offerId });
  }

  inviteWitness(id: string, witnessMemberId: string): Observable<unknown> {
    return this.http.post(`${this.requestsUrl}/${id}/witness-invitations`, { witness_member_id: witnessMemberId });
  }

  respondToWitnessInvitation(
    invitationId: string,
    accept: boolean,
    conflictDeclared: boolean,
    conflictReason?: string,
  ): Observable<unknown> {
    return this.http.post(`${this.requestsUrl}/witness-invitations/${invitationId}/respond`, {
      accept,
      conflict_declared: conflictDeclared,
      conflict_reason: conflictReason,
    });
  }

  scheduleMeeting(id: string, meetingAt: string): Observable<TransferAssistanceRequest> {
    return this.http.post<TransferAssistanceRequest>(`${this.requestsUrl}/${id}/meeting`, { meeting_at: meetingAt });
  }

  confirmHandover(id: string): Observable<TransferAssistanceRequest> {
    return this.http.post<TransferAssistanceRequest>(`${this.requestsUrl}/${id}/handover-confirmation`, {
      confirm_presence_and_handover: true,
    });
  }

  attestWitness(id: string): Observable<TransferAssistanceRequest> {
    return this.http.post<TransferAssistanceRequest>(`${this.requestsUrl}/${id}/witness-attestation`, { attested: true });
  }

  submitEvidence(id: string, provider: string, reference: string): Observable<TransferAssistanceRequest> {
    return this.http.post<TransferAssistanceRequest>(`${this.requestsUrl}/${id}/transfer-evidence`, { provider, reference });
  }

  confirmReceipt(id: string, outcome: 'full' | 'partial' | 'not_received', reason?: string): Observable<TransferAssistanceRequest> {
    return this.http.post<TransferAssistanceRequest>(`${this.requestsUrl}/${id}/receipt-confirmation`, { outcome, reason });
  }

  openDispute(id: string, reason: string): Observable<TransferAssistanceRequest> {
    return this.http.post<TransferAssistanceRequest>(`${this.requestsUrl}/${id}/disputes`, { reason });
  }

  getReviewCases(status: 'disputed' | 'suspended'): Observable<PagedTransferAssistanceReviewCases> {
    return this.http.get<PagedTransferAssistanceReviewCases>(`${this.adminCasesUrl}/cases/${status}`);
  }

  resolveDispute(id: string, resolution: 'resume' | 'cancel', reason: string): Observable<TransferAssistanceRequest> {
    return this.http.post<TransferAssistanceRequest>(`${this.adminCasesUrl}/${id}/resolve-dispute`, { resolution, reason });
  }

  suspend(id: string, reason: string): Observable<TransferAssistanceRequest> {
    return this.http.post<TransferAssistanceRequest>(`${this.adminCasesUrl}/${id}/suspend`, { reason });
  }

  resume(id: string, reason: string): Observable<TransferAssistanceRequest> {
    return this.http.post<TransferAssistanceRequest>(`${this.adminCasesUrl}/${id}/resume`, { reason });
  }

  getEvents(id: string): Observable<TransferAssistanceEvent[]> {
    return this.http.get<TransferAssistanceEvent[]>(`${this.requestsUrl}/${id}/events`);
  }
}
