import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { JobCategory } from '../models/reference-data.models';

@Injectable({ providedIn: 'root' })
export class JobCategoryService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/job-categories`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<JobCategory[]> {
    return this.http.get<JobCategory[]>(this.apiUrl);
  }

  getById(id: string): Observable<JobCategory> {
    return this.http.get<JobCategory>(`${this.apiUrl}/${encodeURIComponent(id)}`);
  }
}
