import { TestBed } from '@angular/core/testing';
import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';
import { AuthResponse, UserProfile } from '../models/auth.models';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: AuthService;
  let router: Router;

  const mockUser: UserProfile = {
    id: '1', firstName: 'Test', lastName: 'User',
    email: 'test@example.com', phoneNumber: '09123456789', roles: ['Candidate']
  };

  const mockAuthResponse: AuthResponse = {
    accessToken: 'new-token',
    expiresAt: new Date(Date.now() + 15 * 60 * 1000).toISOString(),
    user: mockUser
  };

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
  });

  afterEach(() => {
    authService.clearSession();
    sessionStorage.clear();
    httpMock.verify();
  });

  it('should attach the bearer token to outgoing requests', () => {
    authService['accessTokenSignal'].set('existing-token');

    http.get('http://localhost:5158/api/v1/auth/me').subscribe();

    const req = httpMock.expectOne('http://localhost:5158/api/v1/auth/me');
    expect(req.request.headers.get('Authorization')).toBe('Bearer existing-token');
    req.flush(mockUser);
  });

  it('should send credentials (cookies) with every request', () => {
    http.get('http://localhost:5158/api/v1/auth/me').subscribe();

    const req = httpMock.expectOne('http://localhost:5158/api/v1/auth/me');
    expect(req.request.withCredentials).toBeTrue();
    req.flush(mockUser);
  });

  it('should retry a request once after a successful refresh on 401', () => {
    http.get('http://localhost:5158/api/v1/protected').subscribe();

    httpMock.expectOne('http://localhost:5158/api/v1/protected')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    const refreshReq = httpMock.expectOne('http://localhost:5158/api/v1/auth/refresh');
    refreshReq.flush(mockAuthResponse);

    const retried = httpMock.expectOne('http://localhost:5158/api/v1/protected');
    expect(retried.request.headers.get('Authorization')).toBe('Bearer new-token');
    retried.flush({ ok: true });
  });

  it('must not attempt another refresh when the refresh request itself returns 401 (no infinite loop)', () => {
    authService['accessTokenSignal'].set('expired-token');

    http.post('http://localhost:5158/api/v1/auth/refresh', {}).subscribe({
      error: (error: HttpErrorResponse) => expect(error.status).toBe(401)
    });

    httpMock.expectOne('http://localhost:5158/api/v1/auth/refresh')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    httpMock.verify();
    expect(authService.isAuthenticated()).toBeFalse();
  });

  it('should clear the session and redirect to /login when refresh fails', () => {
    authService['accessTokenSignal'].set('expired-token');
    authService['currentUserSignal'].set(mockUser);

    http.get('http://localhost:5158/api/v1/protected').subscribe({
      error: () => void 0
    });

    httpMock.expectOne('http://localhost:5158/api/v1/protected')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    httpMock.expectOne('http://localhost:5158/api/v1/auth/refresh')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(authService.isAuthenticated()).toBeFalse();
    expect(authService.getAccessToken()).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should use one shared refresh request for multiple simultaneous 401 responses', () => {
    const results: unknown[] = [];
    http.get('http://localhost:5158/api/v1/a').subscribe(r => results.push(r));
    http.get('http://localhost:5158/api/v1/b').subscribe(r => results.push(r));

    httpMock.expectOne('http://localhost:5158/api/v1/a')
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    httpMock.expectOne('http://localhost:5158/api/v1/b')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    httpMock.expectOne('http://localhost:5158/api/v1/auth/refresh')
      .flush(mockAuthResponse);

    httpMock.expectOne('http://localhost:5158/api/v1/a').flush({ a: 1 });
    httpMock.expectOne('http://localhost:5158/api/v1/b').flush({ b: 2 });

    expect(results.length).toBe(2);
  });

  it('should not swallow non-401 errors', () => {
    let error: HttpErrorResponse | undefined;

    http.get('http://localhost:5158/api/v1/protected').subscribe({
      error: e => (error = e)
    });

    httpMock.expectOne('http://localhost:5158/api/v1/protected')
      .flush(null, { status: 403, statusText: 'Forbidden' });

    expect(error?.status).toBe(403);
  });
});
