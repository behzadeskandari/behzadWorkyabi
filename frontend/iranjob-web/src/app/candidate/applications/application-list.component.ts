import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JobApplication } from './job-application.models';
import { JobApplicationService } from './job-application.service';

@Component({
  selector: 'app-application-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './application-list.component.html',
  styleUrl: './application-list.component.scss'
})
export class ApplicationListComponent implements OnInit {
  private readonly applicationService = inject(JobApplicationService);

  applications: JobApplication[] = [];
  isLoading = true;
  withdrawingId: string | null = null;
  errorMessage = '';

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.applicationService.getMyApplications().subscribe({
      next: applications => this.applications = applications,
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'بارگذاری درخواست‌های شما ناموفق بود. دوباره تلاش کنید.';
      },
      complete: () => this.isLoading = false
    });
  }

  withdraw(application: JobApplication): void {
    if (application.status !== 'Applied' || this.withdrawingId) return;
    this.withdrawingId = application.id;
    this.errorMessage = '';
    this.applicationService.withdrawApplication(application.id).subscribe({
      next: updated => this.applications = this.applications.map(item => item.id === updated.id ? updated : item),
      error: () => this.errorMessage = 'لغو درخواست ناموفق بود. دوباره تلاش کنید.',
      complete: () => this.withdrawingId = null
    });
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat('fa-IR', { dateStyle: 'medium' }).format(new Date(value));
  }

  statusLabel(status: JobApplication['status']): string {
    return status === 'Withdrawn' ? 'لغوشده' : 'ارسال‌شده';
  }
}
