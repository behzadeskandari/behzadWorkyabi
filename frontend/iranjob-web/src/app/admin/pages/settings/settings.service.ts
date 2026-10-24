import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  AppInfoResponse,
  EmailNotificationsResponse,
  PaymentFeeResponse,
  SettingsResponse,
  UpdateAppInfoRequest,
  UpdateEmailNotificationsRequest,
  UpdatePaymentFeeRequest,
} from './settings.models';

@Injectable({
  providedIn: 'root',
})
export class SettingsService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/super-admin/settings`;

  constructor(private readonly http: HttpClient) {}

  /** Returns all settings sections (Super Admin only). */
  getAllSettings(): Observable<SettingsResponse> {
    return this.http.get<SettingsResponse>(this.apiUrl);
  }

  /** Updates the payment publishing fee (Super Admin only). */
  updatePaymentFee(request: UpdatePaymentFeeRequest): Observable<PaymentFeeResponse> {
    return this.http.put<PaymentFeeResponse>(
      `${this.apiUrl}/payment-fee`,
      request
    );
  }

  /** Updates email notification configuration (Super Admin only). */
  updateEmailNotifications(
    request: UpdateEmailNotificationsRequest
  ): Observable<EmailNotificationsResponse> {
    return this.http.put<EmailNotificationsResponse>(
      `${this.apiUrl}/email-notifications`,
      request
    );
  }

  /** Updates application display name and version (Super Admin only). */
  updateAppInfo(request: UpdateAppInfoRequest): Observable<AppInfoResponse> {
    return this.http.put<AppInfoResponse>(`${this.apiUrl}/app-info`, request);
  }
}