import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { By } from '@angular/platform-browser';
import { JobModerationComponent } from './job-moderation.component';
import { AdminJobPageResult, AdminJobListItem } from '../../models/admin.models';

const API = 'http://localhost:5158/api/v1';

describe('JobModerationComponent', () => {
  let fixture: ComponentFixture<JobModerationComponent>;
  let httpMock: HttpTestingController;
  let component: JobModerationComponent;

  const mockJobs: AdminJobPageResult = {
  items: [
    {
      id: 'job-1',
      title: 'آگهی تست 1',
      categoryId: 'cat-1',
      categoryName: 'فناوری اطلاعات',
      employmentType: 'FullTime',
      workArrangement: 'Remote',
      cityName: 'تهران',
      provinceName: 'تهران',
      countryName: 'ایران',
      companyName: 'شرکت تست',
      employerUserId: 'emp-1',
      salaryMinimum: 50000000,
      salaryMaximum: 100000000,
      salaryCurrency: 'IRR',
      status: 'Published',
      createdAt: '2026-01-15T10:00:00Z',
      publishedAt: '2026-01-20T10:00:00Z',
      closedAt: null,
      moderatedByUserId: 'admin-1',
      moderationReason: null,
      moderatedAt: '2026-01-20T10:00:00Z',
    },
    {
      id: 'job-2',
      title: 'آگهی تست 2',
      categoryId: 'cat-2',
      categoryName: 'طراحی',
      employmentType: 'PartTime',
      workArrangement: 'Hybrid',
      cityName: null,
      provinceName: null,
      countryName: null,
      companyName: 'شرکت دیگر',
      employerUserId: 'emp-2',
      salaryMinimum: null,
      salaryMaximum: null,
      salaryCurrency: null,
      status: 'Draft',
      createdAt: '2026-01-10T10:00:00Z',
      publishedAt: null,
      closedAt: null,
      moderatedByUserId: null,
      moderationReason: null,
      moderatedAt: null,
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
  totalPages: 1,
  };

  const mockCategories = [
    { id: 'cat-1', name: 'فناوری اطلاعات', description: '', isActive: true, createdAt: '2026-01-01T00:00:00Z', updatedAt: null },
    { id: 'cat-2', name: 'طراحی', description: '', isActive: true, createdAt: '2026-01-01T00:00:00Z', updatedAt: null },
  ];

  const mockCountries = [
    { id: 'c1', name: 'ایران', code: 'IR', isActive: true, createdAt: '2026-01-01T00:00:00Z', updatedAt: null },
  ];

  function flushInitialData(): void {
    httpMock.expectOne(API + '/job-categories').flush(mockCategories);
    httpMock.expectOne(API + '/locations/countries').flush(mockCountries);
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [JobModerationComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(JobModerationComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => httpMock.verify());

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should show loading state initially', () => {
    fixture.detectChanges();
    flushInitialData();
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush(mockJobs);
    expect(fixture.nativeElement.textContent).toContain('در حال بارگذاری');
  });

  it('should render jobs table after loading', () => {
    fixture.detectChanges();
    flushInitialData();
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush(mockJobs);
    fixture.detectChanges();

    const rows = fixture.debugElement.queryAll(By.css('.job-table tbody tr'));
    expect(rows.length).toBe(2);
    expect(fixture.nativeElement.textContent).toContain('آگهی تست 1');
  });

  it('should render empty state when no jobs', () => {
    fixture.detectChanges();
    flushInitialData();
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush({
      items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('هیچ آگهی‌ای یافت نشد');
  });

  it('should show status labels in Persian', () => {
    fixture.detectChanges();
    flushInitialData();
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush(mockJobs);
    fixture.detectChanges();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('منتشر شده');
    expect(fixture.nativeElement.textContent).toContain('پیش\u200cنویس');
  });

  it('should display Jalali formatted dates', () => {
    fixture.detectChanges();
    flushInitialData();
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush(mockJobs);
    fixture.detectChanges();
    fixture.detectChanges();

    const dateCells = fixture.debugElement.queryAll(By.css('.job-table td'));
    const createdAtCell = dateCells[6];
    expect(createdAtCell.nativeElement.textContent).not.toBe('-');
  });

  it('should render action buttons based on status', () => {
    fixture.detectChanges();
    flushInitialData();
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush(mockJobs);
    fixture.detectChanges();
    fixture.detectChanges();

    const actionButtons = fixture.debugElement.queryAll(
      By.css('.job-table__actions .btn--action')
    );
    // Published job: unpublish + close, Draft job: publish + close = 4 total
    expect(actionButtons.length).toBe(4);
  });

  it('should filter by status and reset to page 1', () => {
    fixture.detectChanges();
    flushInitialData();
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush(mockJobs);

    component.selectedStatus.set('Draft');
    component.onFilterChange();

    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&status=Draft&sort=Newest').flush(mockJobs);
    fixture.detectChanges();
    expect(component.currentPage()).toBe(1);
  });

  it('should handle pagination navigation', () => {
    fixture.detectChanges();
    flushInitialData();
    const page2Jobs = { ...mockJobs, items: [], totalCount: 50, page: 2, totalPages: 3 };
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush(page2Jobs);

    component.goToPage(3);
    httpMock.expectOne(API + '/admin/jobs?page=3&pageSize=20&sort=Newest').flush(page2Jobs);
  });

  it('should call unpublish when confirmed', () => {
    fixture.detectChanges();
    flushInitialData();
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush(mockJobs);
    fixture.detectChanges();

    spyOn(window, 'confirm').and.returnValue(true);
    component.moderateUnpublish(mockJobs.items[0]);

    const req = httpMock.expectOne(API + '/admin/jobs/job-1/unpublish');
    expect(req.request.method).toBe('POST');
    req.flush(mockJobs.items[0]);
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush(mockJobs);
  });

  it('should not call unpublish when confirm is cancelled', () => {
    fixture.detectChanges();
    flushInitialData();
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush(mockJobs);
    fixture.detectChanges();

    spyOn(window, 'confirm').and.returnValue(false);
    component.moderateUnpublish(mockJobs.items[0]);
    httpMock.expectNone(API + '/admin/jobs/job-1/unpublish');
  });

  it('should call publish when confirmed', () => {
    fixture.detectChanges();
    flushInitialData();
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush(mockJobs);
    fixture.detectChanges();

    spyOn(window, 'confirm').and.returnValue(true);
    component.moderatePublish(mockJobs.items[1]);

    const req = httpMock.expectOne(API + '/admin/jobs/job-2/publish');
    expect(req.request.method).toBe('POST');
    req.flush(mockJobs.items[1]);
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush(mockJobs);
  });

  it('should call close when confirmed', () => {
    fixture.detectChanges();
    flushInitialData();
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush(mockJobs);
    fixture.detectChanges();

    spyOn(window, 'confirm').and.returnValue(true);
    component.moderateClose(mockJobs.items[0]);

    const req = httpMock.expectOne(API + '/admin/jobs/job-1/close');
    expect(req.request.method).toBe('POST');
    req.flush(mockJobs.items[0]);
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush(mockJobs);
  });

  it('should show error state on API failure', () => {
    fixture.detectChanges();
    flushInitialData();
        httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').error(new ErrorEvent('Server error'));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('خطا در بارگذاری');
  });

  it('should load categories and countries for filters', () => {
    fixture.detectChanges();
    flushInitialData();
    httpMock.expectOne(API + '/admin/jobs?page=1&pageSize=20&sort=Newest').flush(mockJobs);
    fixture.detectChanges();
    expect(component.categories().length).toBe(2);
    expect(component.countries().length).toBe(1);
  });

  it('should have canPublish/canUnpublish/canClose methods working', () => {
    const draftJob = { ...mockJobs.items[1] };
    const publishedJob = { ...mockJobs.items[0] };
        const closedJob = { ...mockJobs.items[0], status: 'Closed' as const };

    expect(component.canPublish(draftJob)).toBeTrue();
    expect(component.canUnpublish(draftJob)).toBeFalse();
    expect(component.canPublish(publishedJob)).toBeFalse();
    expect(component.canUnpublish(publishedJob)).toBeTrue();
    expect(component.canClose(closedJob)).toBeFalse();
  });

  it('should format location with Persian commas', () => {
    const job = mockJobs.items[0];
    expect(component.getLocation(job)).toBe('تهران، تهران، ایران');
  });

  it('should return dash for null location', () => {
    const job = mockJobs.items[1];
    expect(component.getLocation(job)).toBe('-');
  });
});
