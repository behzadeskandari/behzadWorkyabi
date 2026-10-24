import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  PaginatedPaymentsResponse,
  PaymentDetail,
  PaymentFilter,
  PaymentSummary,
} from './payments.models';

@Injectable({
  providedIn: 'root',
})
export class PaymentsService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/super-admin/payments`;

  constructor(private readonly http: HttpClient) {}

  /** Returns a paginated, filterable list of all payments (Super Admin only). */
  getAllPayments(
    page: number,
    pageSize: number,
    filter: PaymentFilter = {}
  ): Observable<PaginatedPaymentsResponse> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (filter.search) params = params.set('search', filter.search);
    if (filter.status) params = params.set('status', filter.status);
    if (filter.paymentMethod) params = params.set('paymentMethod', filter.paymentMethod);
    if (filter.fromDate) params = params.set('fromDate', filter.fromDate);
    if (filter.toDate) params = params.set('toDate', filter.toDate);

    return this.http.get<PaginatedPaymentsResponse>(this.apiUrl, { params });
  }

  /** Returns payment summary statistics (Super Admin only). */
  getPaymentSummary(): Observable<PaymentSummary> {
    return this.http.get<PaymentSummary>(`${this.apiUrl}/summary`);
  }

  /** Returns full detail for a single payment by ID (Super Admin only). */
  getPaymentDetail(paymentId: string): Observable<PaymentDetail> {
    return this.http.get<PaymentDetail>(`${this.apiUrl}/${paymentId}`);
  }
}