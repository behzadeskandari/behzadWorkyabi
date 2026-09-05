import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { UserProfile } from '../../models/auth.models';

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './account.component.html',
  styleUrls: ['./account.component.scss']
})
export class AccountComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  userProfile: UserProfile | null = null;
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.authService.getCurrentUser().subscribe({
      next: user => {
        this.userProfile = user;
        this.isLoading = false;
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
}
