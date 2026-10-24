import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EmployerResumeViewResponse, EmployerApplicationDetail, ApplicationStatus } from '../models/employer-application.models';
import { EmployerApplicationsService } from '../services/employer-applications.service';
import { JalaliDateService } from '../../core/services/jalali-date.service';
import { ResumeViewComponent } from '../../shared/components/resume-view/resume-view.component';
import { MessagesService } from '../../messages/messages.service';

@Component({
  selector: 'app-employer-application-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ResumeViewComponent],
  templateUrl: './application-detail.component.html',
  styleUrl: './application-detail.component.scss'
})
export class EmployerApplicationDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly applicationService = inject(EmployerApplicationsService);
  private readonly messages = inject(MessagesService);
  private readonly router = inject(Router);
  isStartingConversation = false;

    application: EmployerApplicationDetail | null = null;
  resume: EmployerResumeViewResponse | null = null;
  isLoadingResume = false;
  resumeError = '';
  isLoading = true;
  isUpdating = false;
  errorMessage = '';

  private readonly jalaliDate = inject(JalaliDateService);

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
      next: app => {
        this.application = app;
        this.loadResume(id);
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'درخواست موردنظر پیدا نشد یا بارگذاری آن ناموفق بود.';
      },
      complete: () => (this.isLoading = false)
    });
  }

  startConversation(): void {
    if (!this.application || this.isStartingConversation) return;
    this.isStartingConversation = true;
    this.messages.create(this.application.jobId, this.application.id).subscribe({
      next: conversation => void this.router.navigate(['/messages', conversation.id]),
      error: () => { this.errorMessage = 'شروع گفتگو ناموفق بود.'; this.isStartingConversation = false; },
      complete: () => this.isStartingConversation = false
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

    loadResume(id: string): void {
    this.isLoadingResume = true;
    this.resumeError = '';
    this.resume = null;
    this.applicationService.getCandidateResume(id).subscribe({
      next: data => (this.resume = data),
      error: () => {
        this.resumeError = 'رزومهٔ کارجو در دسترس نیست یا هنوز به اشتراک گذاشته نشده است.';
      },
      complete: () => (this.isLoadingResume = false)
    });
  }

  downloadCv(id: string): void {
    this.applicationService.downloadCandidateCv(id).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = 'cv.pdf';
        anchor.click();
        URL.revokeObjectURL(url);
      },
      error: () => {
        this.resumeError = 'دریافت فایل رزومه ناموفق بود. لطفاً دوباره تلاش کنید.';
      }
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
