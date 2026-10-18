import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PublicJobDetails } from '../models/public-job.models';
import { PublicJobSearchService } from '../services/public-job-search.service';
import { AuthService } from '../../auth/services/auth.service';
import { JalaliDateService } from '../../core/services/jalali-date.service';
import { JobApplication } from '../../candidate/applications/job-application.models';
import { JobApplicationService } from '../../candidate/applications/job-application.service';
import { CandidateProfileService } from '../../candidate/services/candidate-profile.service';
import { EmploymentTypeService } from '../../reference-data/services/employment-type.service';
import { WorkArrangementService } from '../../reference-data/services/work-arrangement.service';
import { SalaryCurrencyService } from '../../reference-data/services/salary-currency.service';
import { EmploymentType, SalaryCurrency, WorkArrangement } from '../../reference-data/models/reference-data.models';

@Component({
  selector: 'app-job-details',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './job-details.component.html',
  styleUrl: './job-details.component.scss'
})
export class JobDetailsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly searchService = inject(PublicJobSearchService);
    private readonly authService = inject(AuthService);
  private readonly jalaliDate = inject(JalaliDateService);
  private readonly applicationService = inject(JobApplicationService);
  private readonly profileService = inject(CandidateProfileService);
  private readonly employmentTypeService = inject(EmploymentTypeService);
  private readonly workArrangementService = inject(WorkArrangementService);
  private readonly salaryCurrencyService = inject(SalaryCurrencyService);
  private readonly destroyRef = inject(DestroyRef);

  job: PublicJobDetails | null = null;
  isLoading = true;
  errorMessage = '';
  employmentTypes: EmploymentType[] = [];
  workArrangements: WorkArrangement[] = [];
  salaryCurrencies: SalaryCurrency[] = [];
  application: JobApplication | null = null;
  isApplicationStateLoading = false;
  isApplying = false;
  applicationMessage = '';
  isReady = true;
  readinessMessage = '';

  get isCandidate(): boolean {
    return this.authService.hasRole('Candidate');
  }

  get isAnonymous(): boolean {
    return !this.authService.isAuthenticated();
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorMessage = 'این فرصت شغلی پیدا نشد یا دیگر منتشرشده نیست.';
      this.isLoading = false;
      return;
    }
    this.loadReferenceData();
    this.searchService.getById(id).subscribe({
      next: job => {
        this.job = job;
        if (this.isCandidate) {
          this.loadReadiness();
          this.loadApplicationState();
        }
      },
      error: () => {
        this.errorMessage = 'این فرصت شغلی پیدا نشد یا دیگر منتشرشده نیست.';
        this.isLoading = false;
      },
      complete: () => this.isLoading = false
    });
  }

  private loadReferenceData(): void {
    this.employmentTypeService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: items => this.employmentTypes = items,
      error: () => {}
    });
    this.workArrangementService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: items => this.workArrangements = items,
      error: () => {}
    });
    this.salaryCurrencyService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: items => this.salaryCurrencies = items,
      error: () => {}
    });
  }

  apply(): void {
    if (!this.job) return;
    if (this.isAnonymous) {
      void this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }
    if (!this.isCandidate || this.isApplying || this.application) return;
    if (!this.isReady) {
      this.applicationMessage = this.readinessMessage || 'برای ارسال درخواست، ابتدا پروفایل شخصی خود را کامل کنید.';
      return;
    }

    this.isApplying = true;
    this.applicationMessage = '';
    this.applicationService.applyToJob(this.job.id).subscribe({
      next: application => {
        this.application = application;
        this.applicationMessage = 'درخواست همکاری شما با موفقیت ارسال شد.';
      },
      error: error => {
        this.isApplying = false;
        if (error.status === 401) {
          void this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
        } else if (error.status === 409) {
          this.applicationMessage = 'قبلاً برای این موقعیت درخواست داده‌اید.';
          this.loadApplicationState();
        } else if (error.status === 404) {
          this.applicationMessage = 'برای ارسال درخواست، ابتدا پروفایل کارجو را تکمیل کنید یا وضعیت این موقعیت را بررسی کنید.';
        } else {
          this.applicationMessage = 'ارسال درخواست ناموفق بود. دوباره تلاش کنید.';
        }
      },
      complete: () => this.isApplying = false
    });
  }

  private loadReadiness(): void {
    this.profileService.getCompletion().subscribe({
      next: status => {
        this.isReady = status.readiness.isReady;
        this.readinessMessage = status.readiness.message;
      },
      error: () => {
        this.isReady = true;
      }
    });
  }

  private loadApplicationState(): void {
    if (!this.job) return;
    this.isApplicationStateLoading = true;
    this.applicationService.getMyApplications().subscribe({
      next: applications => {
        this.application = applications.find(application => application.jobId === this.job?.id) ?? null;
      },
      error: error => {
        this.isApplicationStateLoading = false;
        if (error.status === 404) {
          this.applicationMessage = 'برای ارسال درخواست، ابتدا پروفایل کارجو را تکمیل کنید.';
        } else if (error.status === 401) {
          void this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
        } else {
          this.applicationMessage = 'بررسی وضعیت درخواست ناموفق بود.';
        }
      },
      complete: () => this.isApplicationStateLoading = false
    });
  }

    formatDate(value: string): string {
    return this.jalaliDate.formatDisplay(value);
  }

  formatSalary(job: PublicJobDetails): string | null {
    if (job.salaryMinimum === null && job.salaryMaximum === null) return null;
    const format = (value: number) => new Intl.NumberFormat('fa-IR').format(value);
    const amount = job.salaryMinimum !== null && job.salaryMaximum !== null
      ? `${format(job.salaryMinimum)} تا ${format(job.salaryMaximum)}`
      : format(job.salaryMinimum ?? job.salaryMaximum ?? 0);
    const period = job.salaryPeriod === 'Monthly' ? ' ماهانه' : job.salaryPeriod === 'Yearly' ? ' سالانه' : '';
    const currency = this.salaryCurrencies.find(c => c.code === job.salaryCurrency);
    const currencyDisplay = currency ? (currency.symbol ?? currency.name) : (job.salaryCurrency ?? '');
    return `${amount} ${currencyDisplay}${period}`.trim();
  }

  arrangementLabel(value: string): string {
    return this.workArrangements.find(a => a.value === value)?.name ?? value;
  }

  employmentLabel(value: string): string {
    return this.employmentTypes.find(t => t.value === value)?.name ?? value;
  }
}
