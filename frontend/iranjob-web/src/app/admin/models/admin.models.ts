export interface SystemStats {
  totalUsers: number;
  activeUsers: number;
  lockedOutUsers: number;
  recentRegistrations: number;
  usersByRole: Record<string, number>;
  generatedAt: string;
}

export interface AdminUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  roles: string[];
}

export interface UserListResponse {
  items: AdminUser[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface UserDetail {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  lockoutEnd: string | null;
  updatedAt: string;
  roles: string[];
}

export interface PasswordResetResult {
  userId: string;
  email: string;
  initiated: boolean;
  errorMessage: string | null;
  token: string | null;
}

export interface SetUserActiveStatusRequest {
  isActive: boolean;
}

export interface SetUserLockoutRequest {
  lockOut: boolean;
}

export interface UpdateUserRolesRequest {
  roles: string[];
}

// ── Phase 14: Job Moderation ──────────────────────────────────────────

export type JobModerationStatus = 'Draft' | 'Published' | 'Closed';

export interface AdminJobListItem {
  id: string;
  title: string;
  categoryId: string;
  categoryName: string;
  employmentType: string;
  workArrangement: string;
  cityName: string | null;
  provinceName: string | null;
  countryName: string | null;
  companyName: string;
  employerUserId: string;
  salaryMinimum: number | null;
  salaryMaximum: number | null;
  salaryCurrency: string | null;
  status: JobModerationStatus;
  createdAt: string;
  publishedAt: string | null;
  closedAt: string | null;
  moderatedByUserId: string | null;
  moderationReason: string | null;
  moderatedAt: string | null;
}

export interface AdminJobPageResult {
  items: AdminJobListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AdminJobSkill {
  id: string;
  name: string;
}

export interface AdminJobDetail {
  id: string;
  title: string;
  description: string;
  categoryId: string;
  categoryName: string;
  skills: AdminJobSkill[];
  employmentType: string;
  workArrangement: string;
  cityId: string | null;
  cityName: string | null;
  provinceName: string | null;
  countryName: string | null;
  salaryMinimum: number | null;
  salaryMaximum: number | null;
  salaryCurrency: string | null;
  salaryPeriod: string | null;
  closingAt: string | null;
  status: JobModerationStatus;
  createdAt: string;
  updatedAt: string | null;
  publishedAt: string | null;
  closedAt: string | null;
  employerUserId: string;
  companyName: string;
  industry: string;
  moderatedByUserId: string | null;
  moderationReason: string | null;
  moderatedAt: string | null;
}

export interface ModerateJobRequest {
  reason?: string | null;
}
