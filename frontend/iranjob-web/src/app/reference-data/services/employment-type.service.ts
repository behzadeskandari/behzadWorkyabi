import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EmploymentType } from '../models/reference-data.models';

@Injectable({ providedIn: 'root' })
export class EmploymentTypeService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/employment-types`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<EmploymentType[]> {
    return this.http.get<EmploymentType[]>(this.apiUrl);
  }
}