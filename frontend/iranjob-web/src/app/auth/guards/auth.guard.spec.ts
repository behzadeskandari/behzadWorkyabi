import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, provideRouter } from '@angular/router';
import { authGuard, roleGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';
import { UserProfile } from '../models/auth.models';

describe('AuthGuard & RoleGuard', () => {
  const mockUser: UserProfile = {
    id: '1',
    firstName: 'Test',
    lastName: 'User',
    email: 'test@example.com',
    phoneNumber: '09123456789',
    roles: ['Candidate']
  };

  const fakeRoute: ActivatedRouteSnapshot = {} as ActivatedRouteSnapshot;
  const state = (url: string) => ({ url }) as RouterStateSnapshot;

  function runGuard(guard: typeof authGuard, route: ActivatedRouteSnapshot, routerState: RouterStateSnapshot) {
    return TestBed.runInInjectionContext(() => guard(route, routerState));
  }

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([])]
    });
  });

  afterEach(() => {
    sessionStorage.clear();
    TestBed.inject(AuthService).clearSession();
  });

  describe('authGuard', () => {
    it('should allow activation for authenticated users', () => {
      const service = TestBed.inject(AuthService);
      (service as unknown as { currentUserSignal: { set: (v: UserProfile | null) => void } })
        .currentUserSignal.set(mockUser);

      const result = runGuard(authGuard, fakeRoute, state('/account'));
      expect(result).toBeTrue();
    });

    it('should redirect anonymous users to /login with a returnUrl', () => {
      const result = runGuard(authGuard, fakeRoute, state('/account')) as URL;

      expect(result.toString()).toBe('/login?returnUrl=%2Faccount');
    });
  });

  describe('roleGuard', () => {
    it('should allow activation when the user has a required role', () => {
      const service = TestBed.inject(AuthService);
      (service as unknown as { currentUserSignal: { set: (v: UserProfile | null) => void } })
        .currentUserSignal.set(mockUser);

      const route = { data: { roles: ['Candidate', 'Employer'] } } as ActivatedRouteSnapshot;
      const result = runGuard(roleGuard, route, state('/account'));
      expect(result).toBeTrue();
    });

    it('should allow activation when no roles are required', () => {
      const service = TestBed.inject(AuthService);
      (service as unknown as { currentUserSignal: { set: (v: UserProfile | null) => void } })
        .currentUserSignal.set(mockUser);

      const result = runGuard(roleGuard, fakeRoute, state('/account'));
      expect(result).toBeTrue();
    });

    it('should redirect to home when the user lacks the required role', () => {
      const service = TestBed.inject(AuthService);
      (service as unknown as { currentUserSignal: { set: (v: UserProfile | null) => void } })
        .currentUserSignal.set(mockUser);

      const route = { data: { roles: ['Admin'] } } as ActivatedRouteSnapshot;
      const result = runGuard(roleGuard, route, state('/account'));
      expect(result.toString()).toBe('/');
    });

    it('should redirect anonymous users to /login', () => {
      const route = { data: { roles: ['Admin'] } } as ActivatedRouteSnapshot;
      const result = runGuard(roleGuard, route, state('/account')) as URL;

      expect(result.toString()).toBe('/login?returnUrl=%2Faccount');
    });
  });
});
