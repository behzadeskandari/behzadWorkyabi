import { Injectable, computed, signal } from '@angular/core';
import { HttpClient, HttpContext, HttpContextToken, HttpHeaders, HttpResponse } from '@angular/common/http';
import { Observable, catchError, finalize, map, shareReplay, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, UserProfile } from '../models/auth.models';

export const SKIP_AUTH_REFRESH = new HttpContextToken(() => false);

const USER_STORAGE_KEY = 'iranjob.currentUser';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/auth`;
  private readonly currentUserSignal = signal<UserProfile | null>(null);
  private readonly accessTokenSignal = signal<string | null>(null);
  private csrfToken: string | null = null;
  private refreshInFlight$?: Observable<AuthResponse>;

  readonly currentUser = computed(() => this.currentUserSignal());
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);
  readonly roles = computed(() => this.currentUserSignal()?.roles ?? []);

  constructor(private readonly http: HttpClient) {
    this.restoreUserProfile();
  }

  register(request: RegisterRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/register`, request, {
      withCredentials: true,
      context: new HttpContext().set(SKIP_AUTH_REFRESH, true)
    });
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request, {
      withCredentials: true,
      observe: 'response',
      context: new HttpContext().set(SKIP_AUTH_REFRESH, true)
    }).pipe(
      map(response => this.captureSession(response))
    );
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/logout`, {}, {
      withCredentials: true,
      headers: this.csrfHeaders(),
      context: new HttpContext().set(SKIP_AUTH_REFRESH, true)
    }).pipe(
      tap(() => this.clearSession()),
      catchError(error => {
        this.clearSession();
        return throwError(() => error);
      })
    );
  }

  getCurrentUser(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.apiUrl}/me`, { withCredentials: true }).pipe(
      tap(user => {
        this.currentUserSignal.set(user);
        this.persistUserProfile(user);
      })
    );
  }

  refresh(): Observable<AuthResponse> {
    if (!this.refreshInFlight$) {
      this.refreshInFlight$ = this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, {}, {
        withCredentials: true,
        observe: 'response',
        headers: this.csrfHeaders(),
        context: new HttpContext().set(SKIP_AUTH_REFRESH, true)
      }).pipe(
        map(response => this.captureSession(response)),
        finalize(() => {
          this.refreshInFlight$ = undefined;
        }),
        shareReplay(1)
      );
    }

    return this.refreshInFlight$;
  }

  getAccessToken(): string | null {
    return this.accessTokenSignal();
  }

  getCsrfToken(): string | null {
    return this.csrfToken;
  }

  hasRole(role: string): boolean {
    return this.roles().includes(role);
  }

  clearSession(): void {
    this.currentUserSignal.set(null);
    this.accessTokenSignal.set(null);
    this.csrfToken = null;
    sessionStorage.removeItem(USER_STORAGE_KEY);
  }

  private captureSession(response: HttpResponse<AuthResponse>): AuthResponse {
    const body = response.body;
    if (!body) {
      throw new Error('Authentication response was empty.');
    }

    const csrf = response.headers.get('X-CSRF-TOKEN');
    if (csrf) {
      this.csrfToken = csrf;
    }

    this.accessTokenSignal.set(body.accessToken);
    this.currentUserSignal.set(body.user);
    this.persistUserProfile(body.user);
    return body;
  }

  private csrfHeaders(): HttpHeaders {
    let headers = new HttpHeaders();
    if (this.csrfToken) {
      headers = headers.set('X-CSRF-TOKEN', this.csrfToken);
    }

    return headers;
  }

  private persistUserProfile(user: UserProfile): void {
    sessionStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user));
  }

  private restoreUserProfile(): void {
    const stored = sessionStorage.getItem(USER_STORAGE_KEY);
    if (!stored) {
      return;
    }

    try {
      this.currentUserSignal.set(JSON.parse(stored) as UserProfile);
    } catch {
      sessionStorage.removeItem(USER_STORAGE_KEY);
    }
  }
}
