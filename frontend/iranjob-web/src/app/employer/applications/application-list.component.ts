import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EmployerApplicationListItem, ApplicationStatus } from '../models/employer-application.models';
import { EmployerApplicationsService } from '../services/employer-applications.service';

@Component({
  selector: 'app-employer-application-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './application-list.component.html',
  styleUrl: './application-list.component.scss'
})
export class EmployerApplicationListComponent implements OnInit {
  private readonly applicationService = inject(EmployerApplicationsService);

  applications: EmployerApplicationListItem[] = [];
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.applicationService.getApplications().subscribe({
      next: apps => this.applications = apps,
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'بارگذاری درخواست‌ها ناموفق بود. دوباره تلاش کنید.';
      },
      complete: () => (this.isLoading = false)
    });
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat('fa-IR', { dateStyle: 'medium' }).format(new Date(value));
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
