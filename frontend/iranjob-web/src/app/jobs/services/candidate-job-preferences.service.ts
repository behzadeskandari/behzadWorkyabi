import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { JobAlert, JobAlertRequest, SavedJobPage } from '../models/candidate-job-preferences.models';

@Injectable({ providedIn: 'root' })
export class CandidateJobPreferencesService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/api/v1/candidates`;
  readonly savedIds = signal<ReadonlySet<string>>(new Set());
  private statusRequest = 0;
  private mutationVersion = 0;

  checkSaved(jobIds: string[]) {
    const request = ++this.statusRequest;
    const mutationAtRequest = this.mutationVersion;
    let params = new HttpParams();
    for (const id of jobIds) params = params.append('jobIds', id);
    return this.http.get<string[]>(`${this.url}/saved-jobs/status`, { params }).pipe(
      tap(ids => {
        if (request === this.statusRequest && mutationAtRequest === this.mutationVersion) {
          this.savedIds.set(new Set(ids));
        }
      })
    );
  }
  listSaved(page = 1, pageSize = 20) { return this.http.get<SavedJobPage>(`${this.url}/saved-jobs`, { params: { page, pageSize } }); }
  save(jobId: string) { return this.http.put<void>(`${this.url}/saved-jobs/${encodeURIComponent(jobId)}`, {}).pipe(tap(() => this.setSaved(jobId, true))); }
  remove(jobId: string) { return this.http.delete<void>(`${this.url}/saved-jobs/${encodeURIComponent(jobId)}`).pipe(tap(() => this.setSaved(jobId, false))); }
  listAlerts() { return this.http.get<JobAlert[]>(`${this.url}/job-alerts`); }
  createAlert(request: JobAlertRequest) { return this.http.post<JobAlert>(`${this.url}/job-alerts`, request); }
  updateAlert(id: string, request: JobAlertRequest) { return this.http.put<JobAlert>(`${this.url}/job-alerts/${encodeURIComponent(id)}`, request); }
  deleteAlert(id: string) { return this.http.delete<void>(`${this.url}/job-alerts/${encodeURIComponent(id)}`); }

  private setSaved(id: string, saved: boolean): void {
    this.mutationVersion++;
    const next = new Set(this.savedIds());
    if (saved) next.add(id); else next.delete(id);
    this.savedIds.set(next);
  }
}
