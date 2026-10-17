import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../services/admin.service';
import { AdminUser, UserListResponse } from '../../models/admin.models';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.scss'],
})
export class UserManagementComponent implements OnInit {
  private readonly adminService = inject(AdminService);

  protected readonly Math = Math;

  users = signal<AdminUser[]>([]);
  totalCount = signal<number>(0);
  currentPage = signal<number>(1);
  pageSize = signal<number>(20);
  isLoading = signal<boolean>(true);
  error = signal<string | null>(null);

  // Filter signals
  searchTerm = signal<string>('');
  public selectedRole = signal<string>('');
  selectedStatus = signal<string>('');

  readonly availableRoles = [
    'Candidate',
    'Employer',
    'Recruiter',
    'Admin',
    'SuperAdmin',
  ];

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading.set(true);
    this.error.set(null);

    const isActive =
      this.selectedStatus() === '' ? undefined : this.selectedStatus() === 'active';

    this.adminService
      .getUsers(
        this.currentPage(),
        this.pageSize(),
        this.searchTerm() || undefined,
        this.selectedRole() || undefined,
        isActive
      )
      .subscribe({
        next: (result: UserListResponse) => {
          this.users.set(result.items);
          this.totalCount.set(result.totalCount);
          this.isLoading.set(false);
        },
        error: () => {
          this.error.set('خطا در بارگذاری کاربران. لطفاً دوباره تلاش کنید.');
          this.isLoading.set(false);
        },
      });
  }

    onSearchChange(): void {
    this.currentPage.set(1);
    this.loadUsers();
  }

  onFilterChange(): void {
    this.currentPage.set(1);
    this.loadUsers();
  }

  onSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchTerm.set(input.value);
  }

  onRoleChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedRole.set(select.value);
    this.onFilterChange();
  }

  onStatusChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedStatus.set(select.value);
    this.onFilterChange();
  }

  goToPage(page: number): void {
    const maxPage = Math.max(1, Math.ceil(this.totalCount() / this.pageSize()));
    this.currentPage.set(Math.min(Math.max(1, page), maxPage));
    this.loadUsers();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount() / this.pageSize()));
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
}
