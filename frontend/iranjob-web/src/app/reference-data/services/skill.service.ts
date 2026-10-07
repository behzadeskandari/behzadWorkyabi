import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Skill } from '../models/reference-data.models';

@Injectable({ providedIn: 'root' })
export class SkillService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/skills`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Skill[]> {
    return this.http.get<Skill[]>(this.apiUrl);
  }

  getById(id: string): Observable<Skill> {
    return this.http.get<Skill>(`${this.apiUrl}/${encodeURIComponent(id)}`);
  }
}
