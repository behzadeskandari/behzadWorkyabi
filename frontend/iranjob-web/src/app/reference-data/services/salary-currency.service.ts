import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SalaryCurrency } from '../models/reference-data.models';

@Injectable({ providedIn: 'root' })
export class SalaryCurrencyService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/salary-currencies`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<SalaryCurrency[]> {
    return this.http.get<SalaryCurrency[]>(this.apiUrl);
  }
}