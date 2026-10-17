import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ResumeResponseDto, SaveResumeRequest, ResumeStatus } from '../models/resume.models';
import { Skill } from '../../reference-data/models/reference-data.models';

@Injectable({ providedIn: 'root' })
export class CandidateResumeService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/candidates/resume`;

  constructor(private readonly http: HttpClient) {}

  getResume(): Observable<ResumeResponseDto> {
    return this.http.get<ResumeResponseDto>(this.apiUrl, { withCredentials: true });
  }

  saveResume(request: SaveResumeRequest): Observable<ResumeResponseDto> {
    return this.http.put<ResumeResponseDto>(this.apiUrl, request, { withCredentials: true });
  }

  setSharingEnabled(share: boolean): Observable<ResumeResponseDto> {
    const status: ResumeStatus = share ? 'ReadyToShare' : 'Complete';
    return this.http.post<ResumeResponseDto>(`${this.apiUrl}/status?status=${status}`, {}, { withCredentials: true });
  }

  getSkills(): Observable<Skill[]> {
    return this.http.get<Skill[]>(`${environment.apiBaseUrl}/api/v1/skills`, { withCredentials: true });
  }

  uploadCv(file: File): Observable<ResumeResponseDto> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ResumeResponseDto>(`${this.apiUrl}/cv`, formData, { withCredentials: true });
  }
}
