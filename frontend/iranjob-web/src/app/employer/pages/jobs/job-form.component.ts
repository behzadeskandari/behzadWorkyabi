import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { JobCategorySelectComponent } from '../../../reference-data/components/job-category-select.component';
import { LocationSelectComponent } from '../../../reference-data/components/location-select.component';
import { SkillSelectComponent } from '../../../reference-data/components/skill-select.component';
import { JalaliDatepickerComponent } from '../../../core/components/jalali-datepicker.component';
import { JalaliDateService } from '../../../core/services/jalali-date.service';
import { EmploymentType, JobPosting, SalaryPeriod, SaveJobPostingRequest, WorkArrangement } from '../../models/job-posting.models';
import { JobPostingService } from '../../services/job-posting.service';

@Component({
  selector: 'app-employer-job-form',
  standalone: true,
    imports: [CommonModule, ReactiveFormsModule, RouterLink, JobCategorySelectComponent, SkillSelectComponent, LocationSelectComponent, JalaliDatepickerComponent],
  templateUrl: './job-form.component.html',
  styleUrl: './job-form.component.scss'
})
export class EmployerJobFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
    private readonly jobService = inject(JobPostingService);
  private readonly jalaliDate = inject(JalaliDateService);

  readonly currencyOptions: Array<{ value: string; label: string }> = [
    { value: 'IRR', label: 'تومان (IRR)' },
    { value: 'USD', label: 'دلار (USD)' },
    { value: 'EUR', label: 'یورو (EUR)' }
  ];


  readonly jobForm = this.fb.group({
    title: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(200)]),
    description: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(10000)]),
    categoryId: this.fb.nonNullable.control('', Validators.required),
    skillIds: this.fb.nonNullable.control<string[]>([], Validators.required),
    employmentType: this.fb.nonNullable.control<EmploymentType>('FullTime', Validators.required),
    workArrangement: this.fb.nonNullable.control<WorkArrangement>('OnSite', Validators.required),
    cityId: this.fb.control<string | null>(null),
    salaryMinimum: this.fb.control<number | null>(null),
    salaryMaximum: this.fb.control<number | null>(null),
        salaryCurrency: this.fb.nonNullable.control('IRR'),
    salaryPeriod: this.fb.nonNullable.control<SalaryPeriod | ''>(''),
        closingAt: this.fb.control<string | null>(null)
  }, { validators: salaryRangeValidator() });

  readonly employmentTypes: Array<{ value: EmploymentType; label: string }> = [
    { value: 'FullTime', label: 'تمام‌وقت' },
    { value: 'PartTime', label: 'پاره‌وقت' },
    { value: 'Contract', label: 'قراردادی' },
    { value: 'Internship', label: 'کارآموزی' },
    { value: 'Temporary', label: 'موقت' }
  ];
  readonly workArrangements: Array<{ value: WorkArrangement; label: string }> = [
    { value: 'OnSite', label: 'حضوری' },
    { value: 'Hybrid', label: 'ترکیبی' },
    { value: 'Remote', label: 'دورکاری' }
  ];
  readonly salaryPeriods: Array<{ value: SalaryPeriod; label: string }> = [
    { value: 'Hourly', label: 'ساعتی' },
    { value: 'Monthly', label: 'ماهانه' },
    { value: 'Yearly', label: 'سالانه' }
  ];
  jobId: string | null = null;
  currentStatus: JobPosting['status'] | null = null;
  isLoading = false;
  isSaving = false;
  errorMessage = '';
  successMessage = '';
  validationErrors: Record<string, string[]> = {};

  ngOnInit(): void {
    this.jobId = this.route.snapshot.paramMap.get('id');
    if (!this.jobId) return;

    this.isLoading = true;
    this.jobService.getById(this.jobId).subscribe({
      next: job => this.patchJob(job),
      error: () => {
        this.errorMessage = 'بارگذاری آگهی ناموفق بود.';
        this.isLoading = false;
      },
      complete: () => this.isLoading = false
    });
  }

  saveDraft(): void {
    if (!this.validateForm()) return;
    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';

    const operation = this.jobId
      ? this.jobService.update(this.jobId, this.toRequest())
      : this.jobService.create(this.toRequest());

    operation.subscribe({
      next: job => {
        this.jobId = job.id;
        this.currentStatus = job.status;
        this.successMessage = 'پیش‌نویس آگهی ذخیره شد.';
        void this.router.navigate(['/employer/jobs']);
      },
      error: error => this.handleError(error),
      complete: () => this.isSaving = false
    });
  }

  publish(): void {
    if (!this.validateForm()) return;
    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';
    const saveOperation = this.jobId
      ? this.jobService.update(this.jobId, this.toRequest())
      : this.jobService.create(this.toRequest());

    saveOperation.pipe(
      switchMap(job => {
        this.jobId = job.id;
        return this.jobService.publish(job.id);
      })
    ).subscribe({
      next: job => {
        this.currentStatus = job.status;
        void this.router.navigate(['/employer/jobs']);
      },
      error: error => this.handleError(error),
      complete: () => this.isSaving = false
    });
  }

  hasError(controlName: string): boolean {
    const control = this.jobForm.get(controlName);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  messagesFor(controlName: string): string[] {
    return this.validationErrors[controlName] ?? [];
  }

  private validateForm(): boolean {
    if (this.jobForm.invalid) {
      this.jobForm.markAllAsTouched();
      return false;
    }
    return true;
  }

  private toRequest(): SaveJobPostingRequest {
    const value = this.jobForm.getRawValue();
    return {
      title: value.title.trim(),
      description: value.description.trim(),
      categoryId: value.categoryId,
      skillIds: value.skillIds,
      employmentType: value.employmentType,
      workArrangement: value.workArrangement,
      cityId: value.cityId,
      salaryMinimum: value.salaryMinimum,
      salaryMaximum: value.salaryMaximum,
      salaryCurrency: value.salaryMinimum !== null || value.salaryMaximum !== null ? value.salaryCurrency.trim() : null,
      salaryPeriod: value.salaryMinimum !== null || value.salaryMaximum !== null ? value.salaryPeriod || null : null,
            closingAt: value.closingAt ? this.jalaliDate.parseJalaliDate(value.closingAt)?.toISOString() ?? null : null
    };
  }

  private patchJob(job: JobPosting): void {
    this.currentStatus = job.status;
    this.jobForm.patchValue({
      title: job.title,
      description: job.description,
      categoryId: job.categoryId,
      skillIds: job.skills.map(skill => skill.id),
      employmentType: job.employmentType,
      workArrangement: job.workArrangement,
      cityId: job.cityId,
      salaryMinimum: job.salaryMinimum,
      salaryMaximum: job.salaryMaximum,
      salaryCurrency: job.salaryCurrency ?? '',
      salaryPeriod: job.salaryPeriod ?? '',
            closingAt: job.closingAt ? this.jalaliDate.toJalaliString(job.closingAt) : ''
    });
  }

  private handleError(error: { status?: number; error?: { errors?: Record<string, string[]> } }): void {
    this.isSaving = false;
    if (error.status === 400 && error.error?.errors) {
      this.validationErrors = error.error.errors;
      this.errorMessage = 'اطلاعات آگهی معتبر نیست.';
    } else if (error.status === 409) {
      this.errorMessage = 'این عملیات با وضعیت فعلی آگهی سازگار نیست.';
    } else if (error.status === 404) {
      this.errorMessage = 'آگهی یا پروفایل کارفرما یافت نشد.';
    } else if (error.status === 403) {
      this.errorMessage = 'دسترسی به مدیریت آگهی ندارید.';
    } else {
      this.errorMessage = 'ذخیره آگهی ناموفق بود. دوباره تلاش کنید.';
    }
  }
}

function salaryRangeValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const minimum = control.get('salaryMinimum')?.value as number | null;
    const maximum = control.get('salaryMaximum')?.value as number | null;
        const currency = (control.get('salaryCurrency')?.value as string).trim();
    const period = control.get('salaryPeriod')?.value as string;
    const cityId = control.get('cityId')?.value as string | null;
        const closingAt = control.get('closingAt')?.value as string | null;
    const errors: ValidationErrors = {};

    if (!cityId) errors['missingCity'] = true;

    const hasSalaryDetails = minimum !== null || maximum !== null;
    if (hasSalaryDetails && (minimum === null || maximum === null || !currency.match(/^[a-z]{3}$/i) || !period)) {
      errors['incompleteSalaryDetails'] = true;
    }

    if (minimum !== null && maximum !== null && (minimum < 0 || maximum < minimum)) {
      errors['invalidSalaryRange'] = true;
    }

    if (closingAt) {
      const closingTimestamp = new Date(`${closingAt}T23:59:59`).getTime();
      if (!Number.isFinite(closingTimestamp) || closingTimestamp <= Date.now()) errors['invalidClosingDate'] = true;
    }

    return Object.keys(errors).length ? errors : null;
  };
}

