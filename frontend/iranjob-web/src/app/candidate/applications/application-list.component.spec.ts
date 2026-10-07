import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { ApplicationListComponent } from './application-list.component';
import { JobApplication } from './job-application.models';
import { JobApplicationService } from './job-application.service';

describe('ApplicationListComponent', () => {
  let fixture: ComponentFixture<ApplicationListComponent>;
  let service: jasmine.SpyObj<JobApplicationService>;
  const application: JobApplication = {
    id: 'application-1', jobId: 'job-1', jobTitle: 'Developer', companyName: 'Example Co',
    status: 'Applied', appliedAt: '2026-09-01T00:00:00Z', updatedAt: null
  };

  beforeEach(() => {
    service = jasmine.createSpyObj<JobApplicationService>('JobApplicationService', ['getMyApplications', 'withdrawApplication']);
    service.getMyApplications.and.returnValue(of([application]));
    TestBed.configureTestingModule({
      imports: [ApplicationListComponent],
      providers: [{ provide: JobApplicationService, useValue: service }, provideRouter([])]
    });
    fixture = TestBed.createComponent(ApplicationListComponent);
  });

  it('shows loading until the application list request completes', () => {
    const pending = new Subject<JobApplication[]>();
    service.getMyApplications.and.returnValue(pending);
    fixture.detectChanges();
    expect(fixture.componentInstance.isLoading).toBeTrue();
    pending.next([]);
    pending.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.isLoading).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('درخواستی ثبت نشده است');
  });

  it('renders the candidate application and its job', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Developer');
    expect(fixture.nativeElement.textContent).toContain('Example Co');
  });

  it('shows an error state when the list cannot be loaded', () => {
    service.getMyApplications.and.returnValue(throwError(() => new Error('offline')));
    fixture.detectChanges();
    expect(fixture.componentInstance.errorMessage).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('updates an application after withdrawal', () => {
    service.withdrawApplication.and.returnValue(of({ ...application, status: 'Withdrawn' }));
    fixture.detectChanges();
    fixture.componentInstance.withdraw(application);
    expect(service.withdrawApplication).toHaveBeenCalledWith(application.id);
    expect(fixture.componentInstance.applications[0].status).toBe('Withdrawn');
  });
});
