import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApplicationDetailComponent } from './application-detail.component';
import { JobApplication } from './job-application.models';
import { JobApplicationService } from './job-application.service';

describe('ApplicationDetailComponent', () => {
  let fixture: ComponentFixture<ApplicationDetailComponent>;
  let service: jasmine.SpyObj<JobApplicationService>;
  const application: JobApplication = {
    id: 'application-1', jobId: 'job-1', jobTitle: 'Developer', companyName: 'Example Co',
    status: 'Applied', appliedAt: '2026-09-01T00:00:00Z', updatedAt: null
  };

  beforeEach(() => {
    service = jasmine.createSpyObj<JobApplicationService>('JobApplicationService', ['getMyApplication', 'withdrawApplication']);
    service.getMyApplication.and.returnValue(of(application));
    TestBed.configureTestingModule({
      imports: [ApplicationDetailComponent],
      providers: [
        provideRouter([]),
        { provide: JobApplicationService, useValue: service },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: application.id }) } } }
      ]
    });
    fixture = TestBed.createComponent(ApplicationDetailComponent);
  });

  it('loads and renders the application detail', () => {
    fixture.detectChanges();
    expect(service.getMyApplication).toHaveBeenCalledWith(application.id);
    expect(fixture.componentInstance.isLoading).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('Developer');
    expect(fixture.nativeElement.textContent).toContain('Example Co');
  });

  it('shows an error state when the application cannot be loaded', () => {
    service.getMyApplication.and.returnValue(throwError(() => new Error('not found')));
    fixture.detectChanges();
    expect(fixture.componentInstance.errorMessage).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('updates the application after withdrawal', () => {
    service.withdrawApplication.and.returnValue(of({ ...application, status: 'Withdrawn' as const }));
    fixture.detectChanges();
    fixture.componentInstance.withdraw();
    expect(service.withdrawApplication).toHaveBeenCalledWith(application.id);
    expect(fixture.componentInstance.application?.status).toBe('Withdrawn');
  });
});
