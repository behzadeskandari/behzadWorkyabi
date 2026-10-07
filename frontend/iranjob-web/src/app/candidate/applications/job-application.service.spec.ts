import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { JobApplicationService } from './job-application.service';
import { environment } from '../../../environments/environment';

describe('JobApplicationService', () => {
  let service: JobApplicationService;
  let http: HttpTestingController;
  const api = `${environment.apiBaseUrl}/api/v1`;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(JobApplicationService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('submits an application without client ownership identifiers', () => {
    service.applyToJob('job-1').subscribe();
    const request = http.expectOne(`${api}/jobs/job-1/applications`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({});
    request.flush({ id: 'application-1' });
  });

  it('loads only the current candidate applications endpoint', () => {
    service.getMyApplications().subscribe();
    const request = http.expectOne(`${api}/candidates/applications`);
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('loads a candidate application by id', () => {
    service.getMyApplication('application-1').subscribe();
    const request = http.expectOne(`${api}/candidates/applications/application-1`);
    expect(request.request.method).toBe('GET');
    request.flush({ id: 'application-1' });
  });

  it('withdraws an application through the candidate endpoint', () => {
    service.withdrawApplication('application-1').subscribe();
    const request = http.expectOne(`${api}/candidates/applications/application-1/withdraw`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({});
    request.flush({ id: 'application-1', status: 'Withdrawn' });
  });
});
