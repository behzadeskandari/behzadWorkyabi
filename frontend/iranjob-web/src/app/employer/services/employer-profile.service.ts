import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EmployerProfile, SaveEmployerProfileRequest } from '../models/employer-profile.models';

@Injectable({
  providedIn: 'root'
})
export class EmployerProfileService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/employers/profile`;

  constructor(private readonly http: HttpClient) {}

  getProfile(): Observable<EmployerProfile> {
    return this.http.get<EmployerProfile>(this.apiUrl, { withCredentials: true });
  }

  createProfile(request: SaveEmployerProfileRequest): Observable<EmployerProfile> {
    return this.http.post<EmployerProfile>(this.apiUrl, request, { withCredentials: true });
  }

    updateProfile(request: SaveEmployerProfileRequest): Observable<EmployerProfile> {
    return this.http.put<EmployerProfile>(this.apiUrl, request, { withCredentials: true });
  }

  uploadLogo(formData: FormData): Observable<{ logoUrl: string }> {
    return this.http.post<{ logoUrl: string }>(`${this.apiUrl}/logo`, formData, { withCredentials: true });
  }
}
