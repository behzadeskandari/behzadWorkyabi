import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Waits for authentication initialization to complete, then evaluates the
 * authentication check. This prevents guards from misinterpreting "still
 * initializing" as "unauthenticated" during application startup.
 */
function waitForAuthReady(): Promise<void> {
  const authService = inject(AuthService);
  if (authService.isAuthReady()) {
    return Promise.resolve();
  }

  return new Promise<void>(resolve => {
    const check = () => authService.isAuthReady() ? resolve() : queueMicrotask(check);
    check();
  });
}

export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthReady()) {
    // Authentication is still being restored — defer the decision rather than
    // treating the user as logged out.
    const deferred = router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    return waitForAuthReady().then(() => {
      if (authService.isAuthenticated()) {
        return true;
      }
      return deferred;
    });
  }

  if (authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const requiredRoles = (route.data?.['roles'] as string[] | undefined) ?? [];

  if (!authService.isAuthReady()) {
    const deferred = router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    return waitForAuthReady().then(() => {
      if (!authService.isAuthenticated()) {
        return deferred;
      }
      if (requiredRoles.length === 0 || requiredRoles.some(role => authService.hasRole(role))) {
        return true;
      }
      return router.createUrlTree(['/']);
    });
  }

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }

  if (requiredRoles.length === 0 || requiredRoles.some(role => authService.hasRole(role))) {
    return true;
  }

  return router.createUrlTree(['/']);
};
