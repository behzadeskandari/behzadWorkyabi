import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../auth/services/auth.service';
import { JobApplication } from '../../candidate/applications/job-application.models';
import { JobApplicationService } from '../../candidate/applications/job-application.service';
import { JobDetailsComponent } from './job-details.component';
import { PublicJobDetails } from '../models/public-job.models';
import { PublicJobSearchService } from '../services/public-job-search.service';

describe('JobDetailsComponent application action', () => {
  const job: PublicJobDetails = {
    id: 'job-1', title: 'Developer', description: 'Build software', category: 'Technology', skills: ['Angular'],
    city: 'Tehran', province: 'Tehran', country: 'Iran', employmentType: 'FullTime', workArrangement: 'Remote',
    salaryMinimum: null, salaryMaximum: null, salaryCurrency: null, salaryPeriod: null, publishedAt: '2026-09-01T00:00:00Z',
    employer: { companyName: 'Example Co', description: null, industry: 'Technology', websiteUrl: null, logoUrl: null }
  };
  const application: JobApplication = {
    id: 'application-1', jobId: job.id, jobTitle: job.title, companyName: 'Example Co',
    status: 'Applied', appliedAt: '2026-09-01T00:00:00Z', updatedAt: null
  };

  function setup(isAuthenticated: boolean, currentApplications: JobApplication[] = []) {
    const search = jasmine.createSpyObj<PublicJobSearchService>('PublicJobSearchService', ['getById']);
    search.getById.and.returnValue(of(job));
    const applications = jasmine.createSpyObj<JobApplicationService>('JobApplicationService', [
      'getMyApplications', 'applyToJob'
    ]);
    applications.getMyApplications.and.returnValue(of(currentApplications));
    applications.applyToJob.and.returnValue(of(application));
    const auth = { hasRole: jasmine.createSpy('hasRole').and.returnValue(isAuthenticated), isAuthenticated: () => isAuthenticated };
    TestBed.configureTestingModule({
      imports: [JobDetailsComponent],
      providers: [
        provideRouter([]),
        { provide: PublicJobSearchService, useValue: search },
        { provide: JobApplicationService, useValue: applications },
        { provide: AuthService, useValue: auth },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: job.id }) } } }
      ]
    });
    const router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    Object.defineProperty(router, 'url', { value: '/jobs/job-1' });
    const fixture = TestBed.createComponent(JobDetailsComponent);
    fixture.detectChanges();
    return { fixture, router, applications };
  }

  it('redirects an anonymous applicant to login with the job return URL', () => {
    const { fixture, router, applications } = setup(false);
    fixture.nativeElement.querySelector('.apply-button').click();
    expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { returnUrl: '/jobs/job-1' } });
    expect(applications.applyToJob).not.toHaveBeenCalled();
  });

  it('applies as a candidate and updates the page state', () => {
    const { fixture, applications } = setup(true);
    fixture.nativeElement.querySelector('.apply-button').click();
    fixture.detectChanges();
    expect(applications.applyToJob).toHaveBeenCalledWith(job.id);
    expect(fixture.componentInstance.application?.status).toBe('Applied');
    expect(fixture.nativeElement.querySelector('.apply-button')).toBeNull();
  });

  it('shows the already-applied state and prevents another submission', () => {
    const { fixture, applications } = setup(true, [application]);
    expect(fixture.componentInstance.application).toEqual(application);
    expect(fixture.nativeElement.querySelector('.apply-button')).toBeNull();
    expect(applications.applyToJob).not.toHaveBeenCalled();
  });
});
