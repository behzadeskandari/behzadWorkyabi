import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../services/admin.service';
import { SystemStats } from '../../models/admin.models';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class AdminDashboardComponent implements OnInit {
  private readonly adminService = inject(AdminService);

  stats: SystemStats | null = null;
  isLoading = true;
  error: string | null = null;

  ngOnInit(): void {
    this.loadStats();
  }

  loadStats(): void {
    this.isLoading = true;
    this.error = null;
    this.adminService.getStats().subscribe({
      next: (stats) => {
        this.stats = stats;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.error = 'خطا در بارگذاری آمار سامانه. لطفاً دوباره تلاش کنید.';
      },
    });
  }

  getRoleLabel(role: string): string {
    const labels: Record<string, string> = {
      Candidate: 'کارجو',
      Employer: 'کارفرما',
      Recruiter: 'استخدام‌کننده',
      Admin: 'مدیر',
      SuperAdmin: 'مدیر ارشد',
    };
    return labels[role] ?? role;
  }

    formatDate(dateString: string | null): string {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString('fa-IR', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    });
  }

  /** KeyValue pipe ordering — preserves insertion order. */
  preserveOrder(): number {
    return 0;
  }

  hasRoleEntries(usersByRole: Record<string, number>): boolean {
    return Object.keys(usersByRole ?? {}).length > 0;
  }
}
