import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../auth/services/auth.service';
import { AdminNavItem } from './admin-nav-models';

@Component({
  selector: 'app-admin-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './admin-sidebar.component.html',
  styleUrls: ['./admin-sidebar.component.scss'],
})
export class AdminSidebarComponent {
  protected readonly authService = inject(AuthService);

  @Input() isOpen = false;

  readonly adminNavItems: AdminNavItem[] = [
    { label: 'داشبورد', path: '/admin', icon: '📊' },
    {
      label: 'کاربران',
      path: '/admin/users',
      icon: '👥',
    },
    {
      label: 'مدیریت داده‌های مرجع',
      path: '/admin/reference-data',
      icon: '📋',
    },
    {
      label: 'مدیریت آگهی‌ها',
      path: '/admin/job-moderation',
      icon: '💼',
    },
  ];

  readonly superAdminNavItems: AdminNavItem[] = [
    {
      label: 'مدیریت پرداخت‌ها',
      path: '/admin/payments',
      icon: '💰',
    },
    {
      label: 'تنظیمات سامانه',
      path: '/admin/settings',
      icon: '⚙️',
    },
  ];

  getRoleName(role: string): string {
    const names: Record<string, string> = {
      Admin: 'مدیر',
      SuperAdmin: 'مدیر ارشد',
    };
    return names[role] ?? role;
  }

  get displayedRoles(): string[] {
    return this.authService.roles().filter((role) =>
      role === 'Admin' || role === 'SuperAdmin'
    );
  }

  isSuperAdmin(): boolean {
    return this.authService.hasRole('SuperAdmin');
  }
}
