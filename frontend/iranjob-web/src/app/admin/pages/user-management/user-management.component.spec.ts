import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { UserManagementComponent } from './user-management.component';
import { UserListResponse } from '../../models/admin.models';

describe('UserManagementComponent', () => {
  let fixture: ComponentFixture<UserManagementComponent>;
  let httpMock: HttpTestingController;

  const mockResponse: UserListResponse = {
    items: [
      {
        id: '1',
        firstName: 'Admin',
        lastName: 'User',
        email: 'admin@test.ir',
        phoneNumber: '09120000001',
        isActive: true,
        createdAt: '2026-01-01T00:00:00Z',
        lastLoginAt: '2026-01-10T00:00:00Z',
        roles: ['Admin'],
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UserManagementComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(UserManagementComponent);
  });

  afterEach(() => httpMock.verify());

    it('should show loading state initially', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('در حال بارگذاری');
    httpMock.expectOne(request => request.url.includes('/admin/users')).flush(mockResponse);
  });

  it('should load and display users', () => {
    fixture.detectChanges();

    httpMock
      .expectOne(
        (req) =>
          req.url === 'http://localhost:5158/api/v1/admin/users' &&
          req.params.get('page') === '1' &&
          req.params.get('pageSize') === '20'
      )
      .flush(mockResponse);

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Admin');
    expect(fixture.nativeElement.textContent).toContain('admin@test.ir');
    expect(fixture.nativeElement.textContent).toContain('مدیر');
    expect(fixture.nativeElement.textContent).toContain('فعال');
  });

  it('should display pagination info', () => {
    fixture.detectChanges();
    httpMock.expectOne(request => request.url.includes('/admin/users')).flush(mockResponse);
    fixture.detectChanges();

        expect(fixture.nativeElement.textContent).toContain('1 کاربر');
    expect(fixture.nativeElement.textContent).toContain('صفحه 1');
  });

      it('should navigate pages correctly', () => {
    fixture.detectChanges();
    httpMock.expectOne(request => request.url.includes('/admin/users')).flush(mockResponse);
    fixture.detectChanges();

    const fixture2 = TestBed.createComponent(UserManagementComponent);
    fixture2.detectChanges();

    // First request from ngOnInit on the second fixture instance
    const largeResponse: UserListResponse = {
      items: mockResponse.items,
      totalCount: 100,
      page: 1,
      pageSize: 20,
    };
    const firstReq = httpMock.expectOne(
      (req) =>
        req.url === 'http://localhost:5158/api/v1/admin/users' &&
        req.params.get('page') === '1'
    );
    firstReq.flush(largeResponse);

    const nextPageResponse: UserListResponse = {
      items: mockResponse.items,
      totalCount: 100,
      page: 2,
      pageSize: 20,
    };

    fixture2.componentInstance.goToPage(2);

    const secondReq = httpMock.expectOne(
      (req) =>
        req.url === 'http://localhost:5158/api/v1/admin/users' &&
        req.params.get('page') === '2'
    );
    secondReq.flush(nextPageResponse);

        expect(fixture2.componentInstance.currentPage()).toBe(2);
  });

  it('should update searchTerm signal on search input', () => {
    fixture.detectChanges();
    httpMock.expectOne(request => request.url.includes('/admin/users')).flush(mockResponse);

    const component = fixture.componentInstance;
    const event = { target: { value: 'testuser' } } as unknown as Event;

    component.onSearchInput(event);

    expect(component.searchTerm()).toBe('testuser');
  });

  it('should reset to page 1 and reload on search change', () => {
    fixture.detectChanges();
    httpMock.expectOne(request => request.url.includes('/admin/users')).flush(mockResponse);

    const component = fixture.componentInstance;
    component.currentPage.set(3);

    component.onSearchChange();

    expect(component.currentPage()).toBe(1);
    httpMock.expectOne(request => request.url.includes('/admin/users')).flush(mockResponse);
  });

  it('should update selectedRole and reload on role change', () => {
    fixture.detectChanges();
    httpMock.expectOne(request => request.url.includes('/admin/users')).flush(mockResponse);

    const component = fixture.componentInstance;
    const event = { target: { value: 'Admin' } } as unknown as Event;

    component.onRoleChange(event);

    expect(component.selectedRole()).toBe('Admin');
    expect(component.currentPage()).toBe(1);
    httpMock.expectOne(request => request.url.includes('/admin/users')).flush(mockResponse);
  });

  it('should update selectedStatus and reload on status change', () => {
    fixture.detectChanges();
    httpMock.expectOne(request => request.url.includes('/admin/users')).flush(mockResponse);

    const component = fixture.componentInstance;
    const event = { target: { value: 'active' } } as unknown as Event;

    component.onStatusChange(event);

    expect(component.selectedStatus()).toBe('active');
    expect(component.currentPage()).toBe(1);
    httpMock.expectOne(request => request.url.includes('/admin/users')).flush(mockResponse);
  });

  it('should pass search, role, and isActive filters in API request', () => {
    fixture.detectChanges();
    httpMock.expectOne(request => request.url.includes('/admin/users')).flush(mockResponse);

    const component = fixture.componentInstance;
    component.searchTerm.set('testadmin');
    component.selectedRole.set('Admin');
    component.selectedStatus.set('active');

    // Trigger a new load which should include the filters
    component.goToPage(1);

    const req = httpMock.expectOne((req) =>
      req.url === 'http://localhost:5158/api/v1/admin/users' &&
      req.params.get('search') === 'testadmin' &&
      req.params.get('role') === 'Admin' &&
      req.params.get('isActive') === 'true'
    );

    expect(req).toBeTruthy();
    req.flush(mockResponse);
  });

  it('should display role labels in Persian', () => {
    const component = fixture.componentInstance;
    expect(component.getRoleLabel('Admin')).toBe('مدیر');
    expect(component.getRoleLabel('SuperAdmin')).toBe('مدیر ارشد');
    expect(component.getRoleLabel('Candidate')).toBe('کارجو');
    expect(component.getRoleLabel('Employer')).toBe('کارفرما');
    expect(component.getRoleLabel('Recruiter')).toBe('استخدام‌کننده');
    expect(component.getRoleLabel('Unknown')).toBe('Unknown');
  });

  it('should format dates using Persian locale', () => {
    const component = fixture.componentInstance;
    const formatted = component.formatDate('2026-01-15T00:00:00Z');
    expect(formatted).not.toBe('-');
    expect(formatted.length).toBeGreaterThan(0);
  });

  it('should return dash for null date in formatDate', () => {
    const component = fixture.componentInstance;
    expect(component.formatDate(null)).toBe('-');
  });
});
