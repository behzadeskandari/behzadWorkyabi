import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PublicJobDetails, PublicJobSearchPage, PublicJobSearchQuery } from '../models/public-job.models';

@Injectable({ providedIn: 'root' })
export class PublicJobSearchService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/jobs`;

  constructor(private readonly http: HttpClient) {}

  search(query: PublicJobSearchQuery): Observable<PublicJobSearchPage> {
    let params = new HttpParams();
    const scalarParams: Record<string, string | number | undefined> = {
      q: query.q,
      categoryId: query.categoryId,
      countryId: query.countryId,
      provinceId: query.provinceId,
      cityId: query.cityId,
      workArrangement: query.workArrangement,
      employmentType: query.employmentType,
      salaryMinimum: query.salaryMinimum,
      salaryMaximum: query.salaryMaximum,
      salaryCurrency: query.salaryCurrency,
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 12,
      sort: query.sort ?? 'Newest'
    };

    for (const [key, value] of Object.entries(scalarParams)) {
      if (value !== undefined && value !== '') params = params.set(key, value);
    }
    for (const skillId of query.skillIds ?? []) params = params.append('skillIds', skillId);

    return this.http.get<PublicJobSearchPage>(this.apiUrl, { params });
  }

  getById(id: string): Observable<PublicJobDetails> {
    return this.http.get<PublicJobDetails>(`${this.apiUrl}/${encodeURIComponent(id)}`);
  }
}
