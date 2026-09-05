import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http';
import { AuthService } from './auth.service';
import { RegisterRequest, LoginRequest, AuthResponse, UserProfile } from '../models/auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should register a new user', () => {
    const request: RegisterRequest = {
      firstName: 'Test',
      lastName: 'User',
      email: 'test@example.com',
      phoneNumber: '09123456789',
      password: 'Password123!',
      role: 'Candidate'
    };

    service.register(request).subscribe();

    const req = httpMock.expectOne('http://localhost:5158/api/v1/auth/register');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(null);
  });

  it('should login a user', () => {
    const request: LoginRequest = {
      identifier: 'test@example.com',
      password: 'Password123!'
    };

    const mockResponse: AuthResponse = {
      accessToken: 'mock-token',
      expiresAt: new Date().toISOString(),
      user: {
        id: '1',
        firstName: 'Test',
        lastName: 'User',
        email: 'test@example.com',
        phoneNumber: '09123456789',
        roles: ['Candidate']
      }
    };

    service.login(request).subscribe(response => {
      expect(response).toEqual(mockResponse);
      expect(service.currentUser).toEqual(mockResponse.user);
      expect(service.isAuthenticated).toBe(true);
    });

    const req = httpMock.expectOne('http://localhost:5158/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });

  it('should logout a user', () => {
    service.logout().subscribe();

    const req = httpMock.expectOne('http://localhost:5158/api/v1/auth/logout');
    expect(req.request.method).toBe('POST');
    req.flush(null);

    expect(service.currentUser).toBeNull();
    expect(service.isAuthenticated).toBe(false);
  });

  it('should get current user', () => {
    const mockUser: UserProfile = {
      id: '1',
      firstName: 'Test',
      lastName: 'User',
      email: 'test@example.com',
      phoneNumber: '09123456789',
      roles: ['Candidate']
    };

    service.getCurrentUser().subscribe(user => {
      expect(user).toEqual(mockUser);
    });

    const req = httpMock.expectOne('http://localhost:5158/api/v1/auth/me');
    expect(req.request.method).toBe('GET');
    req.flush(mockUser);
  });

  it('should check if user has role', () => {
    const mockUser: UserProfile = {
      id: '1',
      firstName: 'Test',
      lastName: 'User',
      email: 'test@example.com',
      phoneNumber: '09123456789',
      roles: ['Candidate', 'Employer']
    };

    service['currentUserSubject'].next(mockUser);

    expect(service.hasRole('Candidate')).toBe(true);
    expect(service.hasRole('Admin')).toBe(false);
  });
});
