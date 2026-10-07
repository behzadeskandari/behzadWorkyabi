import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import { EmployerApplicationsService } from '../services/employer-applications.service';

describe('EmployerApplicationsService', () => {
  let service: EmployerApplicationsService;
  let http: HttpTestingController;
  const api = `${environment.apiBaseUrl}/api/v1/employers/applications`;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(EmployerApplicationsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the employer application list', () => {
    service.getApplications().subscribe();
    const request = http.expectOne(api);
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);
    request.flush([]);
  });

  it('loads an employer application by id', () => {
    service.getApplication('application-1').subscribe();
    const request = http.expectOne(`${api}/application-1`);
    expect(request.request.method).toBe('GET');
    request.flush({ id: 'application-1' });
  });

  it('reviews an application through the employer review endpoint', () => {
    service.reviewApplication('application-1').subscribe();
    const request = http.expectOne(`${api}/application-1/review`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({});
    request.flush({ id: 'application-1', status: 'Reviewed' });
  });

  it('accepts an application through the employer accept endpoint', () => {
    service.acceptApplication('application-1').subscribe();
    const request = http.expectOne(`${api}/application-1/accept`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({});
    request.flush({ id: 'application-1', status: 'Accepted' });
  });

  it('rejects an application through the employer reject endpoint', () => {
    service.rejectApplication('application-1').subscribe();
    const request = http.expectOne(`${api}/application-1/reject`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({});
    request.flush({ id: 'application-1', status: 'Rejected' });
  });
});
