import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EmployerApplicationDetail, EmployerApplicationListItem, EmployerResumeViewResponse } from '../models/employer-application.models';

@Injectable({ providedIn: 'root' })
export class EmployerApplicationsService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/employers/applications`;

  constructor(private readonly http: HttpClient) {}

  getApplications(): Observable<EmployerApplicationListItem[]> {
    return this.http.get<EmployerApplicationListItem[]>(this.apiUrl, { withCredentials: true });
  }

  getApplication(id: string): Observable<EmployerApplicationDetail> {
    return this.http.get<EmployerApplicationDetail>(`${this.apiUrl}/${encodeURIComponent(id)}`, { withCredentials: true });
  }

  reviewApplication(id: string): Observable<EmployerApplicationDetail> {
    return this.http.post<EmployerApplicationDetail>(`${this.apiUrl}/${encodeURIComponent(id)}/review`, {}, { withCredentials: true });
  }

  acceptApplication(id: string): Observable<EmployerApplicationDetail> {
    return this.http.post<EmployerApplicationDetail>(`${this.apiUrl}/${encodeURIComponent(id)}/accept`, {}, { withCredentials: true });
  }

    rejectApplication(id: string): Observable<EmployerApplicationDetail> {
    return this.http.post<EmployerApplicationDetail>(`${this.apiUrl}/${encodeURIComponent(id)}/reject`, {}, { withCredentials: true });
  }

  getCandidateResume(id: string): Observable<EmployerResumeViewResponse> {
    return this.http.get<EmployerResumeViewResponse>(
      `${this.apiUrl}/${encodeURIComponent(id)}/resume`,
      { withCredentials: true });
  }

  downloadCandidateCv(id: string): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/${encodeURIComponent(id)}/cv`,
      { withCredentials: true, responseType: 'blob' });
  }
}
