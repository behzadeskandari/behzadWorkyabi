import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface EmployerPayment {
  id: string;
  employerProfileId: string;
  amount: number;
  currency: string;
  status: 'Pending' | 'Paid' | 'Failed';
  publicationStatus: 'NotRequired' | 'Pending' | 'Completed' | 'Blocked';
  paymentMethod: string;
  authority: string;
  jobPostingId: string | null;
  createdAt: string;
  paidAt: string | null;
  failureReason: string | null;
}

@Injectable({ providedIn: 'root' })
export class EmployerPaymentService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/employers/payments`;

  verifyCallback(authority: string, status: string): Observable<void> {
    const params = new HttpParams().set('authority', authority).set('status', status);
    return this.http.get<void>(`${this.apiUrl}/verify`, { params, withCredentials: true });
  }

  getForEmployer(paymentId: string): Observable<EmployerPayment> {
    return this.http.get<EmployerPayment>(`${this.apiUrl}/${encodeURIComponent(paymentId)}`, { withCredentials: true });
  }
}
