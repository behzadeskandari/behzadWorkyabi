import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { JobPosting, SaveJobPostingRequest } from '../models/job-posting.models';

@Injectable({ providedIn: 'root' })
export class JobPostingService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/employers/jobs`;

  constructor(private readonly http: HttpClient) {}

  getOwn(): Observable<JobPosting[]> {
    return this.http.get<JobPosting[]>(this.apiUrl, { withCredentials: true });
  }

  getById(id: string): Observable<JobPosting> {
    return this.http.get<JobPosting>(`${this.apiUrl}/${encodeURIComponent(id)}`, { withCredentials: true });
  }

  create(request: SaveJobPostingRequest): Observable<JobPosting> {
    return this.http.post<JobPosting>(this.apiUrl, request, { withCredentials: true });
  }

  update(id: string, request: SaveJobPostingRequest): Observable<JobPosting> {
    return this.http.put<JobPosting>(`${this.apiUrl}/${encodeURIComponent(id)}`, request, { withCredentials: true });
  }

  publish(id: string): Observable<JobPosting> {
    return this.http.post<JobPosting>(`${this.apiUrl}/${encodeURIComponent(id)}/publish`, null, { withCredentials: true });
  }

  unpublish(id: string): Observable<JobPosting> {
    return this.http.post<JobPosting>(`${this.apiUrl}/${encodeURIComponent(id)}/unpublish`, null, { withCredentials: true });
  }

  close(id: string): Observable<JobPosting> {
    return this.http.post<JobPosting>(`${this.apiUrl}/${encodeURIComponent(id)}/close`, null, { withCredentials: true });
  }

  publicationAction(job: JobPosting):
    | { kind: 'published' }
    | { kind: 'redirect'; url: string }
    | { kind: 'check-status'; paymentId: string }
    | { kind: 'invalid' } {
    const payment = job.paymentInfo;
    if (!payment)
      return job.status === 'Published' ? { kind: 'published' } : { kind: 'invalid' };
    if (payment.status === 'Paid')
      return { kind: 'check-status', paymentId: payment.paymentId };
    if (payment.status !== 'Pending' || !payment.redirectUrl)
      return { kind: 'invalid' };

    try {
      const redirect = new URL(payment.redirectUrl);
      if (redirect.protocol !== 'https:' || redirect.username || redirect.password)
        return { kind: 'invalid' };
      return { kind: 'redirect', url: redirect.toString() };
    } catch {
      return { kind: 'invalid' };
    }
  }
}
