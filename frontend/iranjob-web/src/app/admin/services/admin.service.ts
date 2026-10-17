import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  SystemStats,
  UserListResponse,
  UserDetail,
  PasswordResetResult,
  SetUserActiveStatusRequest,
  SetUserLockoutRequest,
  UpdateUserRolesRequest,
  AdminJobPageResult,
  AdminJobDetail,
  ModerateJobRequest,
} from '../models/admin.models';

@Injectable({
  providedIn: 'root',
})
export class AdminService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/v1/admin`;

  constructor(private readonly http: HttpClient) {}

  /** Returns real system statistics for the admin dashboard. */
  getStats(): Observable<SystemStats> {
    return this.http.get<SystemStats>(`${this.apiUrl}/stats`);
  }

    /** Returns a paginated list of all users (Admin+ only). */
  getUsers(
    page: number,
    pageSize: number,
    search?: string,
    role?: string,
    isActive?: boolean
  ): Observable<UserListResponse> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) {
      params = params.set('search', search);
    }
    if (role) {
      params = params.set('role', role);
    }
    if (isActive !== undefined && isActive !== null) {
      params = params.set('isActive', isActive);
    }
    return this.http.get<UserListResponse>(`${this.apiUrl}/users`, {
      params,
    });
  }

  /** Returns detailed information for a single user (Admin+ only). */
  getUserById(userId: string): Observable<UserDetail> {
    return this.http.get<UserDetail>(`${this.apiUrl}/users/${userId}`);
  }

  /** Toggles a user's active status (SuperAdmin for privileged users). */
  setUserActiveStatus(
    userId: string,
    isActive: boolean
  ): Observable<void> {
    const body: SetUserActiveStatusRequest = { isActive };
    return this.http.post<void>(
      `${this.apiUrl}/users/${userId}/active`,
      body
    );
  }

  /** Locks or unlocks a user account (SuperAdmin for privileged users). */
  setUserLockout(userId: string, lockOut: boolean): Observable<void> {
    const body: SetUserLockoutRequest = { lockOut };
    return this.http.post<void>(
      `${this.apiUrl}/users/${userId}/lockout`,
      body
    );
  }

  /** Replaces a user's roles (SuperAdmin for privileged users). */
  updateUserRoles(
    userId: string,
    roles: string[]
  ): Observable<void> {
    const body: UpdateUserRolesRequest = { roles };
    return this.http.post<void>(
      `${this.apiUrl}/users/${userId}/roles`,
      body
    );
  }

  /** Initiates a password-reset workflow (no email infra; returns result). */
  initiatePasswordReset(userId: string): Observable<PasswordResetResult> {
    return this.http.post<PasswordResetResult>(
      `${this.apiUrl}/users/${userId}/password-reset`,
      {}
    );
  }

    /** Verifies SuperAdmin-only authorization (used by the Super Admin section). */
  verifySuperAdminAccess(): Observable<{ canManage: boolean; userId: string }> {
    return this.http.get<{ canManage: boolean; userId: string }>(
      `${this.apiUrl}/super-admin/can-manage-users`
    );
  }

  // ── Job Moderation (Admin + SuperAdmin) ──────────────────────────────

  /** Returns a paginated, filterable list of all job postings for moderation. */
  getModeratedJobs(
    page: number,
    pageSize: number,
    search?: string,
    status?: string,
    categoryId?: string,
    cityId?: string,
    employerName?: string,
    sort?: string
  ): Observable<AdminJobPageResult> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (status) params = params.set('status', status);
    if (categoryId) params = params.set('categoryId', categoryId);
    if (cityId) params = params.set('cityId', cityId);
    if (employerName) params = params.set('employerName', employerName);
    if (sort) params = params.set('sort', sort);
    return this.http.get<AdminJobPageResult>(`${this.apiUrl}/jobs`, { params });
  }

  /** Returns full details of a single job posting for moderation review. */
  getModeratedJobById(id: string): Observable<AdminJobDetail> {
    return this.http.get<AdminJobDetail>(`${this.apiUrl}/jobs/${id}`);
  }

  /** Unpublishes a published job posting (takes it down for review). */
  moderateUnpublish(id: string, request: ModerateJobRequest = {}): Observable<AdminJobDetail> {
    return this.http.post<AdminJobDetail>(
      `${this.apiUrl}/jobs/${id}/unpublish`,
      request
    );
  }

  /** Restores a draft job posting back to Published state. */
  moderatePublish(id: string, request: ModerateJobRequest = {}): Observable<AdminJobDetail> {
    return this.http.post<AdminJobDetail>(
      `${this.apiUrl}/jobs/${id}/publish`,
      request
    );
  }

  /** Closes a draft or published job posting. */
  moderateClose(id: string, request: ModerateJobRequest = {}): Observable<AdminJobDetail> {
    return this.http.post<AdminJobDetail>(
      `${this.apiUrl}/jobs/${id}/close`,
      request
    );
  }
}
