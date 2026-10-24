import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Router } from '@angular/router';
import { EmploymentType, JobPosting, JobPostingStatus, WorkArrangement } from '../../models/job-posting.models';
import { JobPostingService } from '../../services/job-posting.service';

@Component({
  selector: 'app-employer-job-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './job-list.component.html',
  styleUrl: './job-list.component.scss'
})
export class EmployerJobListComponent implements OnInit {
  private readonly jobService = inject(JobPostingService);
  private readonly router = inject(Router);

  jobs: JobPosting[] = [];
  isLoading = true;
  pendingJobId: string | null = null;
  errorMessage = '';

  ngOnInit(): void {
    this.loadJobs();
  }

  loadJobs(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.jobService.getOwn().subscribe({
      next: jobs => this.jobs = jobs,
      error: () => {
        this.errorMessage = 'بارگذاری آگهی‌ها ناموفق بود.';
        this.isLoading = false;
      },
      complete: () => this.isLoading = false
    });
  }

  transition(job: JobPosting, action: 'publish' | 'unpublish' | 'close'): void {
    this.pendingJobId = job.id;
    this.errorMessage = '';
    const operation = action === 'publish'
      ? this.jobService.publish(job.id)
      : action === 'unpublish'
        ? this.jobService.unpublish(job.id)
        : this.jobService.close(job.id);

    operation.subscribe({
      next: updated => {
        this.jobs = this.jobs.map(item => item.id === updated.id ? updated : item);
        if (action !== 'publish') return;
        const publication = this.jobService.publicationAction(updated);
        if (publication.kind === 'redirect') {
          window.location.assign(publication.url);
        } else if (publication.kind === 'check-status') {
          void this.router.navigate(['/employer/payments/return'], { queryParams: { paymentId: publication.paymentId } });
        } else if (publication.kind === 'invalid') {
          this.errorMessage = 'انتشار آگهی انجام نشد یا اطلاعات پرداخت کامل نیست. وضعیت آگهی را بررسی کنید.';
        }
      },
      error: () => {
        this.errorMessage = 'تغییر وضعیت آگهی انجام نشد.';
        this.pendingJobId = null;
      },
      complete: () => this.pendingJobId = null
    });
  }

  locationLabel(job: JobPosting): string {
    if (job.workArrangement === 'Remote') return 'دورکاری';
    return [job.cityName, job.provinceName, job.countryName].filter(Boolean).join('، ');
  }

  statusLabel(status: JobPostingStatus): string {
    return status === 'Draft' ? 'پیش‌نویس' : status === 'Published' ? 'منتشرشده' : 'بسته‌شده';
  }

  employmentTypeLabel(type: EmploymentType): string {
    const labels: Record<EmploymentType, string> = {
      FullTime: 'تمام‌وقت',
      PartTime: 'پاره‌وقت',
      Contract: 'قراردادی',
      Internship: 'کارآموزی',
      Temporary: 'موقت'
    };
    return labels[type];
  }

  workArrangementLabel(arrangement: WorkArrangement): string {
    const labels: Record<WorkArrangement, string> = { OnSite: 'حضوری', Hybrid: 'ترکیبی', Remote: 'دورکاری' };
    return labels[arrangement];
  }
}
