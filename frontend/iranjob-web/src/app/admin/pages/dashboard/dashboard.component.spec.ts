import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminDashboardComponent } from './dashboard.component';
import { AdminService } from '../../services/admin.service';
import { SystemStats } from '../../models/admin.models';

describe('AdminDashboardComponent', () => {
  let fixture: ComponentFixture<AdminDashboardComponent>;
  let httpMock: HttpTestingController;

  const mockStats: SystemStats = {
    totalUsers: 150,
    activeUsers: 148,
    lockedOutUsers: 2,
    recentRegistrations: 5,
    usersByRole: { Candidate: 100, Employer: 48, Admin: 1, SuperAdmin: 1 },
    generatedAt: '2026-01-01T00:00:00Z',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminDashboardComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(AdminDashboardComponent);
  });

  afterEach(() => httpMock.verify());

    it('should show loading state initially', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="status"]')).not.toBeNull();
    httpMock.expectOne('http://localhost:5158/api/v1/admin/stats').flush(mockStats);
  });

  it('should load and display system stats', () => {
    fixture.detectChanges();

    httpMock
      .expectOne('http://localhost:5158/api/v1/admin/stats')
      .flush(mockStats);

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('150');
    expect(fixture.nativeElement.textContent).toContain('کل کاربران');
    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeNull();
  });

  it('should show error state on failure', () => {
    fixture.detectChanges();

    httpMock
      .expectOne('http://localhost:5158/api/v1/admin/stats')
      .flush('server error', { status: 500, statusText: 'Server Error' });

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('خطا در بارگذاری');
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('should display role-based user counts', () => {
    fixture.detectChanges();
    httpMock.expectOne('http://localhost:5158/api/v1/admin/stats').flush(mockStats);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('100');
    expect(fixture.nativeElement.textContent).toContain('48');
    expect(fixture.nativeElement.textContent).toContain('کارجو');
    expect(fixture.nativeElement.textContent).toContain('کارفرما');
  });
});
