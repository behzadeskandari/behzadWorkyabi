import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AdminService } from './admin.service';
import {
  SystemStats,
  UserListResponse,
  UserDetail,
  PasswordResetResult,
} from '../models/admin.models';

describe('AdminService', () => {
  let service: AdminService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AdminService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('requests system stats from the versioned admin endpoint', () => {
    const mockStats: SystemStats = {
      totalUsers: 150,
      activeUsers: 148,
      lockedOutUsers: 2,
      recentRegistrations: 5,
      usersByRole: { Candidate: 100, Employer: 48, Admin: 1, SuperAdmin: 1 },
      generatedAt: '2026-01-01T00:00:00Z',
    };

    service.getStats().subscribe((stats) => expect(stats).toEqual(mockStats));

    const req = httpMock.expectOne('http://localhost:5158/api/v1/admin/stats');
    expect(req.request.method).toBe('GET');
    req.flush(mockStats);
  });

  it('requests paginated users with query parameters', () => {
    const mockResponse: UserListResponse = {
      items: [],
      totalCount: 0,
      page: 2,
      pageSize: 10,
    };

    service.getUsers(2, 10).subscribe((result) => {
      expect(result.page).toBe(2);
      expect(result.pageSize).toBe(10);
    });

    const req = httpMock.expectOne(
      (req) =>
        req.url === 'http://localhost:5158/api/v1/admin/users' &&
        req.params.get('page') === '2' &&
        req.params.get('pageSize') === '10'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('passes search, role, and isActive filters as query parameters', () => {
    service
      .getUsers(1, 20, 'ali', 'Admin', true)
      .subscribe();

    const req = httpMock.expectOne(
      (req) =>
        req.url === 'http://localhost:5158/api/v1/admin/users' &&
        req.params.get('page') === '1' &&
        req.params.get('pageSize') === '20' &&
        req.params.get('search') === 'ali' &&
        req.params.get('role') === 'Admin' &&
        req.params.get('isActive') === 'true'
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20 });
  });

  it('verifies super admin access', () => {
    const mockResponse = { canManage: true, userId: 'admin-user-id' };

    service.verifySuperAdminAccess().subscribe((result) => {
      expect(result.canManage).toBeTrue();
    });

    const req = httpMock.expectOne(
      'http://localhost:5158/api/v1/admin/super-admin/can-manage-users'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('requests user details by id', () => {
    const mockUser: UserDetail = {
      id: 'user-123',
      firstName: 'John',
      lastName: 'Doe',
      email: 'john@test.ir',
      phoneNumber: '091200000001',
      isActive: true,
      createdAt: '2026-01-01T00:00:00Z',
      lastLoginAt: '2026-01-10T00:00:00Z',
      lockoutEnd: null,
      updatedAt: '2026-01-10T00:00:00Z',
      roles: ['Candidate'],
    };

    service.getUserById('user-123').subscribe((user) => expect(user).toEqual(mockUser));

    const req = httpMock.expectOne('http://localhost:5158/api/v1/admin/users/user-123');
    expect(req.request.method).toBe('GET');
    req.flush(mockUser);
  });

  it('posts to set user active status', () => {
    service.setUserActiveStatus('user-123', false).subscribe();

    const req = httpMock.expectOne(
      'http://localhost:5158/api/v1/admin/users/user-123/active'
    );
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ isActive: false });
    req.flush({});
  });

  it('posts to set user lockout', () => {
    service.setUserLockout('user-123', true).subscribe();

    const req = httpMock.expectOne(
      'http://localhost:5158/api/v1/admin/users/user-123/lockout'
    );
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ lockOut: true });
    req.flush({});
  });

  it('posts to update user roles', () => {
    service.updateUserRoles('user-123', ['Employer']).subscribe();

    const req = httpMock.expectOne(
      'http://localhost:5158/api/v1/admin/users/user-123/roles'
    );
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ roles: ['Employer'] });
    req.flush({});
  });

  it('posts to initiate password reset', () => {
    const mockResult: PasswordResetResult = {
      userId: 'user-123',
      email: 'john@test.ir',
      initiated: false,
      errorMessage: 'No email delivery mechanism is configured.',
      token: null,
    };

    service.initiatePasswordReset('user-123').subscribe((result) =>
      expect(result).toEqual(mockResult)
    );

    const req = httpMock.expectOne(
      'http://localhost:5158/api/v1/admin/users/user-123/password-reset'
    );
    expect(req.request.method).toBe('POST');
    req.flush(mockResult);
  });
});
