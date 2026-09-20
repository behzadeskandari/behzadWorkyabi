import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http';
import { AuthService } from './auth.service';
import { AuthResponse, LoginRequest, RegisterRequest, UserProfile } from '../models/auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  const mockUser: UserProfile = {
    id: '1', firstName: 'Test', lastName: 'User',
    email: 'test@example.com', phoneNumber: '09123456789', roles: ['Candidate']
  };

  const mockAuthResponse: AuthResponse = {
    accessToken: 'mock-token',
    expiresAt: new Date(Date.now() + 15 * 60 * 1000).toISOString(),
    user: mockUser
  };

  function loginAndFlush(user: UserProfile = mockUser): void {
    service.login({ identifier: 'test@example.com', password: 'Password123!' }).subscribe();
    httpMock.expectOne('http://localhost:5158/api/v1/auth/login')
      .flush({ ...mockAuthResponse, user });
  }

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    service.clearSession();
    sessionStorage.clear();
    httpMock.verify();
  });

  it('should be created and start unauthenticated', () => {
    expect(service).toBeTruthy();
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('should register a new user', () => {
    const request: RegisterRequest = {
      firstName: 'Test', lastName: 'User', email: 'test@example.com',
      phoneNumber: '09123456789', password: 'Password123!', role: 'Candidate'
    };

    service.register(request).subscribe(result => expect(result).toBeNull());

    const req = httpMock.expectOne('http://localhost:5158/api/v1/auth/register');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    expect(req.request.withCredentials).toBeTrue();
    req.flush(null);
  });

  it('should login a user and populate authentication state', () => {
    const request: LoginRequest = { identifier: 'test@example.com', password: 'Password123!' };

    service.login(request).subscribe(response => expect(response).toEqual(mockAuthResponse));

    const req = httpMock.expectOne('http://localhost:5158/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBeTrue();
    req.flush(mockAuthResponse);

    expect(service.isAuthenticated()).toBeTrue();
    expect(service.currentUser()).toEqual(mockUser);
    expect(service.roles()).toEqual(['Candidate']);
    expect(service.getAccessToken()).toBe('mock-token');
  });

  it('should capture the CSRF token from the login response headers', () => {
    service.login({ identifier: 'test@example.com', password: 'Password123!' }).subscribe();

    httpMock.expectOne('http://localhost:5158/api/v1/auth/login')
      .flush(mockAuthResponse, { headers: { 'X-CSRF-TOKEN': 'csrf-123' } });

    expect(service.getCsrfToken()).toBe('csrf-123');
  });

  it('should logout and clear authentication state', () => {
    loginAndFlush();
    expect(service.isAuthenticated()).toBeTrue();

    service.logout().subscribe();

    const req = httpMock.expectOne('http://localhost:5158/api/v1/auth/logout');
    expect(req.request.method).toBe('POST');
    req.flush(null);

    expect(service.currentUser()).toBeNull();
    expect(service.isAuthenticated()).toBeFalse();
    expect(service.getAccessToken()).toBeNull();
  });

  it('should clear local state even when the logout call fails', () => {
    loginAndFlush();

    service.logout().subscribe({ error: () => void 0 });
    httpMock.expectOne('http://localhost:5158/api/v1/auth/logout')
      .flush(null, { status: 500, statusText: 'Server Error' });

    expect(service.isAuthenticated()).toBeFalse();
  });

  it('should get the current user and update state', () => {
    service.getCurrentUser().subscribe(user => expect(user).toEqual(mockUser));

    const req = httpMock.expectOne('http://localhost:5158/api/v1/auth/me');
    expect(req.request.method).toBe('GET');
    req.flush(mockUser);

    expect(service.isAuthenticated()).toBeTrue();
    expect(service.currentUser()).toEqual(mockUser);
  });

  it('should refresh the session using the refresh cookie', () => {
    service.refresh().subscribe(response => expect(response.accessToken).toBe('refreshed-token'));

    const req = httpMock.expectOne('http://localhost:5158/api/v1/auth/refresh');
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBeTrue();
    req.flush({ ...mockAuthResponse, accessToken: 'refreshed-token' });

    expect(service.getAccessToken()).toBe('refreshed-token');
  });

  it('should share a single in-flight refresh request across subscribers', () => {
    const results: AuthResponse[] = [];
    service.refresh().subscribe(r => results.push(r));
    service.refresh().subscribe(r => results.push(r));

    httpMock.expectOne('http://localhost:5158/api/v1/auth/refresh').flush(mockAuthResponse);

    expect(results.length).toBe(2);
    expect(results[0]).toEqual(mockAuthResponse);
    expect(results[1]).toEqual(mockAuthResponse);
  });

  it('should check whether the user has a role', () => {
    loginAndFlush({ ...mockUser, roles: ['Candidate', 'Employer'] });

    expect(service.hasRole('Candidate')).toBeTrue();
    expect(service.hasRole('Employer')).toBeTrue();
    expect(service.hasRole('Admin')).toBeFalse();
  });

  it('should restore the user profile from sessionStorage on construction', () => {
    loginAndFlush();

    const restored = TestBed.inject(AuthService);
    expect(restored.currentUser()).toEqual(mockUser);
  });
});

