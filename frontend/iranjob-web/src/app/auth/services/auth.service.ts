import { Injectable, computed, signal } from '@angular/core';
import { HttpClient, HttpContext, HttpContextToken, HttpHeaders, HttpResponse } from '@angular/common/http';
import { Observable, catchError, finalize, map, shareReplay, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, UserProfile } from '../models/auth.models';

export const SKIP_AUTH_REFRESH = new HttpContextToken(() => false);

const USER_STORAGE_KEY = 'iranjob.currentUser';

/**
 * Persisted authentication session stored in sessionStorage so that a full
 * browser reload can restore the complete authentication state — not just the
 * user profile but also the access token and CSRF token.
 */
interface PersistedSession {
  user: UserProfile;
  accessToken: string;
  csrfToken: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/auth`;
  private readonly currentUserSignal = signal<UserProfile | null>(null);
  private readonly accessTokenSignal = signal<string | null>(null);
  private readonly isAuthReadySignal = signal(false);
  private csrfToken: string | null = null;
  private refreshInFlight$?: Observable<AuthResponse>;

  readonly currentUser = computed(() => this.currentUserSignal());
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);
  readonly roles = computed(() => this.currentUserSignal()?.roles ?? []);

  /**
   * True once the authentication state has been fully restored from storage.
   * Route guards should wait for this to become true before deciding the user
   * is unauthenticated, so they never misinterpret "initializing" as "logged out".
   */
  readonly isAuthReady = computed(() => this.isAuthReadySignal());

  constructor(private readonly http: HttpClient) {
    this.restoreSession();
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
        const token = this.accessTokenSignal();
        if (token) {
          this.persistSession(user, token, this.csrfToken);
        }
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
    this.isAuthReadySignal.set(true);
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
    this.persistSession(body.user, body.accessToken, this.csrfToken);
    return body;
  }

  private csrfHeaders(): HttpHeaders {
    let headers = new HttpHeaders();
    if (this.csrfToken) {
      headers = headers.set('X-CSRF-TOKEN', this.csrfToken);
  }

    return headers;
  }

  /**
   * Persist the complete authentication session (user profile, access token,
   * and CSRF token) so that a full page reload can restore all of it.
   */
  private persistSession(user: UserProfile, accessToken: string, csrfToken: string | null): void {
    const session: PersistedSession = { user, accessToken, csrfToken };
    sessionStorage.setItem(USER_STORAGE_KEY, JSON.stringify(session));
  }

  /**
   * Restore the complete authentication session from sessionStorage.
   * Called synchronously in the constructor so that by the time route guards
   * evaluate, the in-memory state matches what was persisted at login.
   */
  private restoreSession(): void {
    const stored = sessionStorage.getItem(USER_STORAGE_KEY);
    if (!stored) {
      this.isAuthReadySignal.set(true);
      return;
  }

    try {
      const session = JSON.parse(stored) as PersistedSession;
      if (!session.user || !session.accessToken) {
        // Malformed — treat as unauthenticated and clean up
        sessionStorage.removeItem(USER_STORAGE_KEY);
        this.isAuthReadySignal.set(true);
        return;
      }
      this.currentUserSignal.set(session.user);
      this.accessTokenSignal.set(session.accessToken);
      this.csrfToken = session.csrfToken;
    } catch {
      sessionStorage.removeItem(USER_STORAGE_KEY);
  }

    this.isAuthReadySignal.set(true);
  }
}
