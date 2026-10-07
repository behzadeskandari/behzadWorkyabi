import { ApplicationConfig, provideZoneChangeDetection, provideAppInitializer, inject } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { AuthService } from './auth/services/auth.service';
import { correlationInterceptor } from './core/interceptors/correlation.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { authInterceptor } from './auth/interceptors/auth.interceptor';

/**
 * Ensure the AuthService (and thus its sessionStorage restoration logic) is
 * instantiated during application bootstrap, before any route guard runs.
 * Because restoreSession() is synchronous (reads sessionStorage), this
 * completes immediately — the initializer exists to guarantee ordering.
 */
function initializeAuth(): () => void {
  return () => {
    inject(AuthService);
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([correlationInterceptor, errorInterceptor, authInterceptor])),
    provideAppInitializer(initializeAuth())
  ]
};
