import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { UserProfile } from '../../models/auth.models';
import { JalaliDateService } from '../../../core/services/jalali-date.service';

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './account.component.html',
  styleUrls: ['./account.component.scss']
})
export class AccountComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly jalaliDate = inject(JalaliDateService);

  userProfile: UserProfile | null = null;
  isLoading = true;
  errorMessage = '';
  memberSince = '';

    ngOnInit(): void {
    this.authService.getCurrentUser().subscribe({
      next: user => {
        this.userProfile = user;
        this.isLoading = false;
        this.decodeMemberSince();
      },
      error: error => {
        this.isLoading = false;
        if (error.status === 401) {
          void this.router.navigate(['/login']);
        } else {
          this.errorMessage = 'خطا در بارگذاری اطلاعات کاربری';
        }
      }
    });
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => void this.router.navigate(['/login']),
      error: () => void this.router.navigate(['/login'])
    });
  }

    getRoleName(role: string): string {
    const roleNames: Record<string, string> = {
      Candidate: 'کارجو',
      Employer: 'کارفرما',
      Recruiter: 'استخدام‌کننده',
      Admin: 'مدیر',
      SuperAdmin: 'مدیر ارشد'
    };
    return roleNames[role] || role;
  }

    getProfileRoute(role: string): string | null {
    if (role === 'Candidate') return '/candidate/profile';
    if (role === 'Employer') return '/employer/profile';
    return null;
  }

  hasProfileLink(): boolean {
    return this.userProfile?.roles.some(role => this.getProfileRoute(role) !== null) ?? false;
  }

  private decodeMemberSince(): void {
    const token = this.authService.getAccessToken();
    if (!token) {
      this.memberSince = '';
      return;
    }
    try {
      const payload = token.split('.')[1];
      if (!payload) {
        this.memberSince = '';
        return;
      }
            const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      const parsed = JSON.parse(decoded);
      if (parsed.iat) {
        const iatDate = new Date(parsed.iat * 1000);
        this.memberSince = this.jalaliDate.formatDisplay(iatDate);
      }
    } catch {
      this.memberSince = '';
    }
  }
}
