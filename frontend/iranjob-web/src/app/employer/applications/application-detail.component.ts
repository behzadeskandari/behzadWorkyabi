import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EmployerApplicationDetail, ApplicationStatus } from '../models/employer-application.models';
import { EmployerApplicationsService } from '../services/employer-applications.service';

@Component({
  selector: 'app-employer-application-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './application-detail.component.html',
  styleUrl: './application-detail.component.scss'
})
export class EmployerApplicationDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly applicationService = inject(EmployerApplicationsService);

  application: EmployerApplicationDetail | null = null;
  isLoading = true;
  isUpdating = false;
  errorMessage = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorMessage = 'درخواست موردنظر پیدا نشد.';
      this.isLoading = false;
      return;
    }
    this.load(id);
  }

  load(id: string): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.applicationService.getApplication(id).subscribe({
      next: app => (this.application = app),
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'درخواست موردنظر پیدا نشد یا بارگذاری آن ناموفق بود.';
      },
      complete: () => (this.isLoading = false)
    });
  }

  updateStatus(action: 'review' | 'accept' | 'reject'): void {
    if (!this.application || this.isUpdating) return;

    this.isUpdating = true;
    this.errorMessage = '';

    const operation = action === 'review'
      ? this.applicationService.reviewApplication(this.application.id)
      : action === 'accept'
        ? this.applicationService.acceptApplication(this.application.id)
        : this.applicationService.rejectApplication(this.application.id);

    operation.subscribe({
      next: updated => (this.application = updated),
      error: () => {
        this.errorMessage = 'بروزرسانی وضعیت درخواست ناموفق بود. دوباره تلاش کنید.';
      },
      complete: () => (this.isUpdating = false)
    });
  }

  canReview(status: ApplicationStatus): boolean {
    return status === 'Applied';
  }

  canAccept(status: ApplicationStatus): boolean {
    return status === 'Applied' || status === 'Reviewed';
  }

  canReject(status: ApplicationStatus): boolean {
    return status === 'Applied' || status === 'Reviewed';
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat('fa-IR', { dateStyle: 'long' }).format(new Date(value));
  }

  statusLabel(status: ApplicationStatus): string {
    const labels: Record<ApplicationStatus, string> = {
      Applied: 'ارسال‌شده',
      Withdrawn: 'لغوشده',
      Reviewed: 'بررسی‌شده',
      Accepted: 'پذیرفته‌شده',
      Rejected: 'رد‌شده'
    };
    return labels[status];
  }

  statusClass(status: ApplicationStatus): string {
    if (status === 'Withdrawn' || status === 'Rejected') return 'status-badge--inactive';
    if (status === 'Accepted') return 'status-badge--accepted';
    return 'status-badge--active';
  }
}
