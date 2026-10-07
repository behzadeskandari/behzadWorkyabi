import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { City, Country, Location, Province } from '../models/reference-data.models';

@Injectable({ providedIn: 'root' })
export class LocationService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/locations`;

  constructor(private readonly http: HttpClient) {}

  getById(id: string): Observable<Location> {
    return this.http.get<Location>(`${this.apiUrl}/${encodeURIComponent(id)}`);
  }

  getCountries(): Observable<Country[]> {
    return this.http.get<Country[]>(`${this.apiUrl}/countries`);
  }

  getProvinces(countryId: string): Observable<Province[]> {
    const params = new HttpParams().set('countryId', countryId);
    return this.http.get<Province[]>(`${this.apiUrl}/provinces`, { params });
  }

  getCities(provinceId: string): Observable<City[]> {
    const params = new HttpParams().set('provinceId', provinceId);
    return this.http.get<City[]>(`${this.apiUrl}/cities`, { params });
  }
}
