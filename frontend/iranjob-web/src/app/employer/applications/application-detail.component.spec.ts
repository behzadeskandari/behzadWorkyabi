import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { EmployerApplicationDetailComponent } from './application-detail.component';
import { EmployerApplicationDetail } from '../models/employer-application.models';
import { EmployerApplicationsService } from '../services/employer-applications.service';

describe('EmployerApplicationDetailComponent', () => {
  let fixture: ComponentFixture<EmployerApplicationDetailComponent>;
  let service: jasmine.SpyObj<EmployerApplicationsService>;
  const application: EmployerApplicationDetail = {
    id: 'app-1', jobId: 'job-1', jobTitle: 'Frontend Developer',
    jobDescription: 'We need a senior developer.',
    candidateProfileId: 'candidate-1', candidateHeadline: 'Frontend Engineer',
    candidateBiography: 'Senior frontend dev.', candidateCity: 'Tehran', candidateProvince: 'Tehran',
    candidateLinkedInUrl: 'https://linkedin.com/in/test', candidateGitHubUrl: null,
    candidatePortfolioUrl: null, candidateExpectedSalary: 50000, candidateSalaryType: 'Monthly',
    candidateEmploymentStatus: 'Employed', candidateAvailability: 'Immediate',
    candidateMilitaryStatus: null, status: 'Applied',
    appliedAt: '2026-09-01T00:00:00Z', updatedAt: null
  };

  beforeEach(() => {
    service = jasmine.createSpyObj<EmployerApplicationsService>('EmployerApplicationsService', ['getApplication']);
    service.getApplication.and.returnValue(of(application));
    TestBed.configureTestingModule({
      imports: [EmployerApplicationDetailComponent],
      providers: [
        provideRouter([]),
        { provide: EmployerApplicationsService, useValue: service },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: application.id }) } } }
      ]
    });
    fixture = TestBed.createComponent(EmployerApplicationDetailComponent);
  });

  it('loads and renders the application detail and candidate profile', () => {
    fixture.detectChanges();
    expect(service.getApplication).toHaveBeenCalledWith(application.id);
    expect(fixture.componentInstance.isLoading).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('Frontend Developer');
    expect(fixture.nativeElement.textContent).toContain('Frontend Engineer');
    expect(fixture.nativeElement.textContent).toContain('Senior frontend dev.');
  });

  it('shows an error state when the application cannot be loaded', () => {
    service.getApplication.and.returnValue(throwError(() => new Error('not found')));
    fixture.detectChanges();
    expect(fixture.componentInstance.errorMessage).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('shows action buttons for Applied status', () => {
    fixture.detectChanges();
    expect(fixture.componentInstance.canReview('Applied')).toBeTrue();
    expect(fixture.componentInstance.canAccept('Applied')).toBeTrue();
    expect(fixture.componentInstance.canReject('Applied')).toBeTrue();
  });

  it('hides action buttons for Accepted status', () => {
    service.getApplication.and.returnValue(of({ ...application, status: 'Accepted' as const }));
    fixture.detectChanges();
    expect(fixture.componentInstance.canReview('Accepted')).toBeFalse();
    expect(fixture.componentInstance.canAccept('Accepted')).toBeFalse();
    expect(fixture.componentInstance.canReject('Accepted')).toBeFalse();
  });

  it('hides action buttons for Rejected status', () => {
    service.getApplication.and.returnValue(of({ ...application, status: 'Rejected' as const }));
    fixture.detectChanges();
    expect(fixture.componentInstance.canReview('Rejected')).toBeFalse();
    expect(fixture.componentInstance.canAccept('Rejected')).toBeFalse();
    expect(fixture.componentInstance.canReject('Rejected')).toBeFalse();
  });
});
