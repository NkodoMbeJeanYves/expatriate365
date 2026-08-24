import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  PagedPaymentsResult,
  Payment,
  PaymentStats,
  RecordPaymentRequest,
  ReversePaymentRequest,
} from '@models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/payments`;

  getPayments(page = 1, limit = 20, memberId?: string, status?: string, from?: string, to?: string): Observable<PagedPaymentsResult> {
    let params = new HttpParams().set('page', page).set('limit', limit);
    if (memberId) params = params.set('member_id', memberId);
    if (status) params = params.set('status', status);
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<PagedPaymentsResult>(this.base, { params });
  }

  getStats(): Observable<PaymentStats> {
    return this.http.get<PaymentStats>(`${this.base}/stats`);
  }

  getById(id: string): Observable<Payment> {
    return this.http.get<Payment>(`${this.base}/${id}`);
  }

  record(dto: RecordPaymentRequest): Observable<Payment> {
    return this.http.post<Payment>(this.base, dto);
  }

  confirm(id: string): Observable<Payment> {
    return this.http.post<Payment>(`${this.base}/${id}/confirm`, {});
  }

  reverse(id: string, dto: ReversePaymentRequest): Observable<Payment> {
    return this.http.post<Payment>(`${this.base}/${id}/reverse`, dto);
  }

  uploadReceipt(id: string, file: File): Observable<{ receipt_file_url: string }> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<{ receipt_file_url: string }>(`${this.base}/${id}/receipt`, fd);
  }
}
