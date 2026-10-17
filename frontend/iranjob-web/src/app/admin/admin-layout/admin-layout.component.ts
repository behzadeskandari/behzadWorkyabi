import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '../../auth/services/auth.service';
import { AdminSidebarComponent } from '../components/admin-sidebar/admin-sidebar.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, CommonModule, AdminSidebarComponent],
  templateUrl: './admin-layout.component.html',
  styleUrls: ['./admin-layout.component.scss'],
})
export class AdminLayoutComponent {
  private readonly authService = inject(AuthService);

  readonly isSidebarOpen = signal(false);

  get displayName(): string {
    const user = this.authService.currentUser();
    if (!user) return '';
    return `${user.firstName} ${user.lastName}`;
  }

  get roleLabel(): string {
    const roles = this.authService.roles();
    const primary = roles.includes('SuperAdmin')
      ? 'مدیر ارشد'
      : roles.includes('Admin')
        ? 'مدیر'
        : '';
    return primary;
  }

  toggleSidebar(): void {
    this.isSidebarOpen.update((open) => !open);
  }

  closeSidebar(): void {
    this.isSidebarOpen.set(false);
  }

  logout(): void {
    this.authService.logout().subscribe();
  }
}
