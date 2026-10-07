import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { EmployerApplicationListComponent } from './application-list.component';
import { EmployerApplicationListItem } from '../models/employer-application.models';
import { EmployerApplicationsService } from '../services/employer-applications.service';

describe('EmployerApplicationListComponent', () => {
  let fixture: ComponentFixture<EmployerApplicationListComponent>;
  let service: jasmine.SpyObj<EmployerApplicationsService>;
  const application: EmployerApplicationListItem = {
    id: 'app-1', jobId: 'job-1', jobTitle: 'Frontend Developer',
    candidateProfileId: 'candidate-1', candidateHeadline: 'Frontend Engineer',
    candidateCity: 'Tehran', status: 'Applied', appliedAt: '2026-09-01T00:00:00Z', updatedAt: null
  };

  beforeEach(() => {
    service = jasmine.createSpyObj<EmployerApplicationsService>('EmployerApplicationsService', ['getApplications']);
    service.getApplications.and.returnValue(of([application]));
    TestBed.configureTestingModule({
      imports: [EmployerApplicationListComponent],
      providers: [{ provide: EmployerApplicationsService, useValue: service }, provideRouter([])]
    });
    fixture = TestBed.createComponent(EmployerApplicationListComponent);
  });

  it('shows loading until the application list request completes', () => {
    const pending = new Subject<EmployerApplicationListItem[]>();
    service.getApplications.and.returnValue(pending);
    fixture.detectChanges();
    expect(fixture.componentInstance.isLoading).toBeTrue();
    pending.next([]);
    pending.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.isLoading).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('درخواستی دریافت نشده است');
  });

  it('renders the employer application and its job', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Frontend Developer');
    expect(fixture.nativeElement.textContent).toContain('Frontend Engineer');
  });

  it('shows an error state when the list cannot be loaded', () => {
    service.getApplications.and.returnValue(throwError(() => new Error('offline')));
    fixture.detectChanges();
    expect(fixture.componentInstance.errorMessage).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('shows empty state when no applications exist', () => {
    service.getApplications.and.returnValue(of([]));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('درخواستی دریافت نشده است');
  });
});
