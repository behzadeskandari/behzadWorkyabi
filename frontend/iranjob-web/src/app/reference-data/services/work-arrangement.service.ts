import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { WorkArrangement } from '../models/reference-data.models';

@Injectable({ providedIn: 'root' })
export class WorkArrangementService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/work-arrangements`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<WorkArrangement[]> {
    return this.http.get<WorkArrangement[]>(this.apiUrl);
  }
}