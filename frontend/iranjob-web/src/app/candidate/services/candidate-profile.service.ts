import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CandidateProfile, SaveCandidateProfileRequest } from '../models/candidate-profile.models';

@Injectable({
  providedIn: 'root'
})
export class CandidateProfileService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/candidates/profile`;

  constructor(private readonly http: HttpClient) {}

  getProfile(): Observable<CandidateProfile> {
    return this.http.get<CandidateProfile>(this.apiUrl, { withCredentials: true });
  }

  createProfile(request: SaveCandidateProfileRequest): Observable<CandidateProfile> {
    return this.http.post<CandidateProfile>(this.apiUrl, request, { withCredentials: true });
  }

  updateProfile(request: SaveCandidateProfileRequest): Observable<CandidateProfile> {
    return this.http.put<CandidateProfile>(this.apiUrl, request, { withCredentials: true });
  }
}
