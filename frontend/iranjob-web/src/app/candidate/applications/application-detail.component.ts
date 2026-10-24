import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { JobApplication } from './job-application.models';
import { JobApplicationService } from './job-application.service';
import { MessagesService } from '../../messages/messages.service';

@Component({
  selector: 'app-application-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './application-detail.component.html',
  styleUrl: './application-detail.component.scss'
})
export class ApplicationDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly applicationService = inject(JobApplicationService);
  private readonly messages = inject(MessagesService);
  private readonly router = inject(Router);

  application: JobApplication | null = null;
  isLoading = true;
  isWithdrawing = false;
  errorMessage = '';
  isStartingConversation = false;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorMessage = 'درخواست موردنظر پیدا نشد.';
      this.isLoading = false;
      return;
    }
    this.applicationService.getMyApplication(id).subscribe({
      next: application => this.application = application,
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'درخواست موردنظر پیدا نشد یا بارگذاری آن ناموفق بود.';
      },
      complete: () => this.isLoading = false
    });
  }

  withdraw(): void {
    if (!this.application || this.application.status !== 'Applied' || this.isWithdrawing) return;
    this.isWithdrawing = true;
    this.applicationService.withdrawApplication(this.application.id).subscribe({
      next: application => this.application = application,
      error: () => {
        this.errorMessage = 'لغو درخواست ناموفق بود. دوباره تلاش کنید.';
        this.isWithdrawing = false;
      },
      complete: () => this.isWithdrawing = false
    });
  }

  startConversation(): void {
    if (!this.application || this.application.status === 'Withdrawn' || this.isStartingConversation) return;
    this.isStartingConversation = true;
    this.messages.create(this.application.jobId, this.application.id).subscribe({
      next: conversation => void this.router.navigate(['/messages', conversation.id]),
      error: () => { this.errorMessage = 'شروع گفتگو ناموفق بود.'; this.isStartingConversation = false; },
      complete: () => this.isStartingConversation = false
    });
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat('fa-IR', { dateStyle: 'long' }).format(new Date(value));
  }
}
