import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { JobApplication } from './job-application.models';

@Injectable({ providedIn: 'root' })
export class JobApplicationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1`;

  applyToJob(jobId: string): Observable<JobApplication> {
    return this.http.post<JobApplication>(`${this.apiUrl}/jobs/${jobId}/applications`, {});
  }

  getMyApplications(): Observable<JobApplication[]> {
    return this.http.get<JobApplication[]>(`${this.apiUrl}/candidates/applications`);
  }

  getMyApplication(id: string): Observable<JobApplication> {
    return this.http.get<JobApplication>(`${this.apiUrl}/candidates/applications/${id}`);
  }

  withdrawApplication(id: string): Observable<JobApplication> {
    return this.http.post<JobApplication>(`${this.apiUrl}/candidates/applications/${id}/withdraw`, {});
  }
}
