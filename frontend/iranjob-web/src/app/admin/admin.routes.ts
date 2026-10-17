import { Routes } from '@angular/router';
import { authGuard, roleGuard } from '../auth/guards/auth.guard';

export const adminRoutes: Routes = [
  {
    path: '',
    title: 'داشبورد ادمین',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SuperAdmin'] },
    loadComponent: () =>
      import('./pages/dashboard/dashboard.component').then(
        (m) => m.AdminDashboardComponent
      ),
  },
  {
    path: 'users',
    title: 'مدیریت کاربران',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SuperAdmin'] },
    loadComponent: () =>
      import('./pages/user-management/user-management.component').then(
        (m) => m.UserManagementComponent
      ),
  },
  {
    path: 'reference-data',
    title: 'مدیریت داده‌های مرجع',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SuperAdmin'] },
    loadComponent: () =>
      import('./pages/reference-data/reference-data.component').then(
        (m) => m.ReferenceDataManagementComponent
      ),
  },
  {
    path: 'job-moderation',
    title: 'مدیریت آگهی‌ها',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SuperAdmin'] },
    loadComponent: () =>
      import('./pages/job-moderation/job-moderation.component').then(
        (m) => m.JobModerationComponent
      ),
  },
];
