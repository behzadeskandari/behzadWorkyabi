import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { City, Country, JobCategory, Province, Skill } from '../../reference-data/models/reference-data.models';

/**
 * Administrative CRUD service for reference data.
 *
 * Wraps the existing backend API endpoints under `/api/v1/locations`,
 * `/api/v1/job-categories`, and `/api/v1/skills`.
 * Write operations are protected by server-side Admin/SuperAdmin authorization.
 */
@Injectable({
  providedIn: 'root',
})
export class AdminReferenceDataService {
  private readonly locationUrl = `${environment.apiBaseUrl}/api/v1/locations`;
  private readonly categoryUrl = `${environment.apiBaseUrl}/api/v1/job-categories`;
  private readonly skillUrl = `${environment.apiBaseUrl}/api/v1/skills`;

  constructor(private readonly http: HttpClient) {}

  // ── Countries ─────────────────────────────────────────────

  getAllCountries(): Observable<Country[]> {
    return this.http.get<Country[]>(this.locationUrl + '/countries');
  }

  getCountry(id: string): Observable<Country> {
    return this.http.get<Country>(`${this.locationUrl}/countries/${encodeURIComponent(id)}`);
  }

  createCountry(input: { name: string; code: string }): Observable<Country> {
    return this.http.post<Country>(this.locationUrl + '/countries', input);
  }

  updateCountry(id: string, input: { name: string; code: string }): Observable<Country> {
    return this.http.put<Country>(`${this.locationUrl}/countries/${encodeURIComponent(id)}`, input);
  }

  deactivateCountry(id: string): Observable<void> {
    return this.http.delete<void>(`${this.locationUrl}/countries/${encodeURIComponent(id)}`);
  }

  // ── Provinces ─────────────────────────────────────────────

  getAllProvinces(countryId: string): Observable<Province[]> {
    const params = new HttpParams().set('countryId', countryId);
    return this.http.get<Province[]>(this.locationUrl + '/provinces', { params });
  }

  getProvince(id: string): Observable<Province> {
    return this.http.get<Province>(`${this.locationUrl}/provinces/${encodeURIComponent(id)}`);
  }

  createProvince(input: { countryId: string; name: string }): Observable<Province> {
    return this.http.post<Province>(this.locationUrl + '/provinces', input);
  }

  updateProvince(id: string, input: { countryId: string; name: string }): Observable<Province> {
    return this.http.put<Province>(`${this.locationUrl}/provinces/${encodeURIComponent(id)}`, input);
  }

  deactivateProvince(id: string): Observable<void> {
    return this.http.delete<void>(`${this.locationUrl}/provinces/${encodeURIComponent(id)}`);
  }

  // ── Cities ────────────────────────────────────────────────

  getCitiesByProvince(provinceId: string): Observable<City[]> {
    const params = new HttpParams().set('provinceId', provinceId);
    return this.http.get<City[]>(this.locationUrl + '/cities', { params });
  }

  getCity(id: string): Observable<City> {
    return this.http.get<City>(`${this.locationUrl}/cities/${encodeURIComponent(id)}`);
  }

  createCity(input: { provinceId: string; name: string }): Observable<City> {
    return this.http.post<City>(this.locationUrl + '/cities', input);
  }

  updateCity(id: string, input: { provinceId: string; name: string }): Observable<City> {
    return this.http.put<City>(`${this.locationUrl}/cities/${encodeURIComponent(id)}`, input);
  }

  deactivateCity(id: string): Observable<void> {
    return this.http.delete<void>(`${this.locationUrl}/cities/${encodeURIComponent(id)}`);
  }

  // ── Job Categories ────────────────────────────────────────

  getAllCategories(): Observable<JobCategory[]> {
    return this.http.get<JobCategory[]>(this.categoryUrl);
  }

  getCategory(id: string): Observable<JobCategory> {
    return this.http.get<JobCategory>(`${this.categoryUrl}/${encodeURIComponent(id)}`);
  }

  createCategory(input: { name: string; description: string | null }): Observable<JobCategory> {
    return this.http.post<JobCategory>(this.categoryUrl, input);
  }

  updateCategory(id: string, input: { name: string; description: string | null }): Observable<JobCategory> {
    return this.http.put<JobCategory>(`${this.categoryUrl}/${encodeURIComponent(id)}`, input);
  }

  deactivateCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.categoryUrl}/${encodeURIComponent(id)}`);
  }

  // ── Skills ────────────────────────────────────────────────

  getAllSkills(): Observable<Skill[]> {
    return this.http.get<Skill[]>(this.skillUrl);
  }

  getSkill(id: string): Observable<Skill> {
    return this.http.get<Skill>(`${this.skillUrl}/${encodeURIComponent(id)}`);
  }

  createSkill(input: { name: string; description: string | null }): Observable<Skill> {
    return this.http.post<Skill>(this.skillUrl, input);
  }

  updateSkill(id: string, input: { name: string; description: string | null }): Observable<Skill> {
    return this.http.put<Skill>(`${this.skillUrl}/${encodeURIComponent(id)}`, input);
  }

  deactivateSkill(id: string): Observable<void> {
    return this.http.delete<void>(`${this.skillUrl}/${encodeURIComponent(id)}`);
  }
}
