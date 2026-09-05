import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService, SKIP_AUTH_REFRESH } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  let authenticatedRequest = request.clone({ withCredentials: true });
  const accessToken = authService.getAccessToken();
  if (accessToken) {
    authenticatedRequest = authenticatedRequest.clone({
      setHeaders: {
        Authorization: `Bearer ${accessToken}`
      }
    });
  }

  return next(authenticatedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      const skipRefresh = request.context.get(SKIP_AUTH_REFRESH) || request.url.includes('/auth/refresh');
      if (error.status !== 401 || skipRefresh) {
        return throwError(() => error);
      }

      return authService.refresh().pipe(
        switchMap(response => {
          const retried = authenticatedRequest.clone({
            setHeaders: {
              Authorization: `Bearer ${response.accessToken}`
            }
          });
          return next(retried);
        }),
        catchError(refreshError => {
          authService.clearSession();
          void router.navigate(['/login']);
          return throwError(() => refreshError);
        })
      );
    })
  );
};
