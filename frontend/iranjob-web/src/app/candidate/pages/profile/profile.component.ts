import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { JalaliDatepickerComponent } from '../../../core/components/jalali-datepicker.component';
import { JalaliDateService } from '../../../core/services/jalali-date.service';
import { CandidateProfileService } from '../../services/candidate-profile.service';
import {
  availabilityOptions,
  employmentStatusOptions,
  genderOptions,
  militaryStatusOptions,
  salaryTypeOptions
} from '../../models/candidate-profile.models';

@Component({
  selector: 'app-profile',
  standalone: true,
        imports: [CommonModule, ReactiveFormsModule, JalaliDatepickerComponent],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly profileService = inject(CandidateProfileService);
    private readonly router = inject(Router);
  private readonly jalaliDate = inject(JalaliDateService);

  readonly profileForm = this.fb.nonNullable.group({
    headline: ['', [Validators.maxLength(200)]],
    biography: ['', [Validators.maxLength(4000)]],
    dateOfBirth: [''],
    gender: [''],
    city: ['', [Validators.maxLength(100)]],
    province: ['', [Validators.maxLength(100)]],
    linkedInUrl: ['', [Validators.maxLength(500), Validators.pattern(/^https?:\/\/[^\s]+$/)]],
    gitHubUrl: ['', [Validators.maxLength(500), Validators.pattern(/^https?:\/\/[^\s]+$/)]],
    portfolioUrl: ['', [Validators.maxLength(500), Validators.pattern(/^https?:\/\/[^\s]+$/)]],
    expectedSalary: [null as number | null, [Validators.min(0), Validators.max(1_000_000_000)]],
    salaryType: [''],
    employmentStatus: [''],
    availability: [''],
    militaryStatus: ['']
  });

  readonly genderOptions = genderOptions;
  readonly salaryTypeOptions = salaryTypeOptions;
  readonly employmentStatusOptions = employmentStatusOptions;
  readonly availabilityOptions = availabilityOptions;
  readonly militaryStatusOptions = militaryStatusOptions;

    isLoading = true;
  isSaving = false;
  profileExists = false;
  isEditing = false;
  errorMessage = '';
  successMessage = '';
  validationErrors: Record<string, string[]> = {};

  ngOnInit(): void {
    this.profileService.getProfile().subscribe({
      next: profile => {
        this.profileForm.patchValue({
          headline: profile.headline ?? '',
          biography: profile.biography ?? '',
          dateOfBirth: profile.dateOfBirth ?? '',
          gender: profile.gender ?? '',
          city: profile.city ?? '',
          province: profile.province ?? '',
          linkedInUrl: profile.linkedInUrl ?? '',
          gitHubUrl: profile.gitHubUrl ?? '',
          portfolioUrl: profile.portfolioUrl ?? '',
          expectedSalary: profile.expectedSalary,
          salaryType: profile.salaryType ?? '',
          employmentStatus: profile.employmentStatus ?? '',
          availability: profile.availability ?? '',
          militaryStatus: profile.militaryStatus ?? ''
        });
        this.profileExists = true;
        this.isLoading = false;
      },
      error: error => {
        this.isLoading = false;
                if (error.status === 404) {
          this.profileExists = false;
          this.isEditing = true; // No profile yet: go straight to edit/create mode
        } else if (error.status === 401) {
          void this.router.navigate(['/login'], { queryParams: { returnUrl: '/candidate/profile' } });
        } else {
          this.errorMessage = 'خطا در بارگذاری پروفایل. لطفاً دوباره تلاش کنید.';
        }
      }
    });
  }

  save(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.validationErrors = {};

    const value = this.profileForm.getRawValue();
    const request = {
      headline: this.toNullOrTrimmed(value.headline),
      biography: this.toNullOrTrimmed(value.biography),
      dateOfBirth: value.dateOfBirth ? value.dateOfBirth : null,
      gender: value.gender ? value.gender : null,
      city: this.toNullOrTrimmed(value.city),
      province: this.toNullOrTrimmed(value.province),
      linkedInUrl: this.toNullOrTrimmed(value.linkedInUrl),
      gitHubUrl: this.toNullOrTrimmed(value.gitHubUrl),
      portfolioUrl: this.toNullOrTrimmed(value.portfolioUrl),
      expectedSalary: value.expectedSalary,
      salaryType: value.salaryType ? value.salaryType : null,
      employmentStatus: value.employmentStatus ? value.employmentStatus : null,
      availability: value.availability ? value.availability : null,
      militaryStatus: value.militaryStatus ? value.militaryStatus : null
    };

    const operation$ = this.profileExists
      ? this.profileService.updateProfile(request)
      : this.profileService.createProfile(request);

    operation$.subscribe({
            next: () => {
        this.isSaving = false;
        this.profileExists = true;
        this.isEditing = false;
        this.successMessage = 'پروفایل با موفقیت ذخیره شد.';
      },
      error: error => {
        this.isSaving = false;
        if (error.status === 401) {
          void this.router.navigate(['/login'], { queryParams: { returnUrl: '/candidate/profile' } });
        } else if (error.status === 409) {
          this.errorMessage = 'پروفایل شما قبلاً ایجاد شده است.';
          this.profileExists = true;
        } else if (error.status === 404) {
          this.errorMessage = 'پروفایلی برای ذخیره‌سازی یافت نشد. لطفاً صفحه را دوباره بارگذاری کنید.';
          this.profileExists = false;
        } else if (error.status === 400) {
          this.errorMessage = 'اطلاعات وارد شده معتبر نیست.';
          const errors = error.error?.errors as Record<string, string[]> | undefined;
          if (errors) {
            this.validationErrors = errors;
          }
        } else {
          this.errorMessage = 'ذخیره‌سازی پروفایل ناموفق بود. لطفاً دوباره تلاش کنید.';
        }
      }
    });
  }

    enableEditing(): void {
    this.isEditing = true;
    this.errorMessage = '';
    this.successMessage = '';
  }

  cancelEditing(): void {
    this.isEditing = false;
    this.errorMessage = '';
    this.successMessage = '';
  }

  hasError(controlName: string): boolean {
    const control = this.profileForm.get(controlName);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  getValidationMessages(controlName: string): string[] {
    return this.validationErrors[controlName] ?? [];
  }

      formatDate(value: string | null): string {
    if (!value) return '—';
    return this.jalaliDate.formatDisplay(value);
  }

  genderLabel(value: string | null): string {
    if (!value) return '';
    return this.genderOptions.find(option => option.value === value)?.label ?? value;
  }

  salaryTypeLabel(value: string | null): string {
    if (!value) return '';
    return this.salaryTypeOptions.find(option => option.value === value)?.label ?? value;
  }

  employmentStatusLabel(value: string | null): string {
    if (!value) return '';
    return this.employmentStatusOptions.find(option => option.value === value)?.label ?? value;
  }

  availabilityLabel(value: string | null): string {
    if (!value) return '';
    return this.availabilityOptions.find(option => option.value === value)?.label ?? value;
  }

  militaryStatusLabel(value: string | null): string {
    if (!value) return '';
    return this.militaryStatusOptions.find(option => option.value === value)?.label ?? value;
  }

  private toNullOrTrimmed(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }
}

