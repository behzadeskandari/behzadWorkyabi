import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { companySizeOptions, industryOptions } from '../../models/employer-profile.models';
import { EmployerProfileService } from '../../services/employer-profile.service';
import { JalaliDateService } from '../../../core/services/jalali-date.service';

@Component({
  selector: 'app-employer-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class EmployerProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly profileService = inject(EmployerProfileService);
  private readonly jalaliDate = inject(JalaliDateService);

  readonly profileForm = this.fb.nonNullable.group({
    companyName: ['', [Validators.required, Validators.maxLength(200)]],
    industry: ['', [Validators.required, Validators.maxLength(100)]],
    companySize: ['', [Validators.required, Validators.maxLength(50)]],
    city: ['', [Validators.required, Validators.maxLength(100)]],
    province: ['', [Validators.required, Validators.maxLength(100)]],
    companyDescription: ['', [Validators.maxLength(2000)]],
    websiteUrl: ['', [Validators.maxLength(500), Validators.pattern(/^https?:\/\/[^\s]+$/)]],
    linkedInUrl: ['', [Validators.maxLength(500), Validators.pattern(/^https?:\/\/[^\s]+$/)]],
    logoUrl: ['', [Validators.maxLength(500), Validators.pattern(/^https?:\/\/[^\s]+$/)]],
    foundedYear: [null as number | null, [Validators.min(1800), Validators.max(new Date().getFullYear())]],
    address: ['', [Validators.maxLength(500)]],
    postalCode: ['', [Validators.maxLength(20)]],
    contactEmail: ['', [Validators.email, Validators.maxLength(255)]],
    contactPhone: ['', [Validators.maxLength(50)]]
  });

  readonly companySizeOptions = companySizeOptions;
  readonly industryOptions = industryOptions;
  readonly currentYear = new Date().getFullYear();
  createdAt: string | null = null;
  updatedAt: string | null = null;

  isLoading = true;
  isSaving = false;
  isUploadingLogo = false;
  profileExists = false;
  isEditing = false;
  errorMessage = '';
  successMessage = '';
  validationErrors: Record<string, string[]> = {};

  ngOnInit(): void {
    this.profileService.getProfile().subscribe({
      next: profile => {
        this.profileForm.patchValue({
          companyName: profile.companyName ?? '',
          companyDescription: profile.companyDescription ?? '',
          industry: profile.industry ?? '',
          companySize: profile.companySize ?? '',
          websiteUrl: profile.websiteUrl ?? '',
          linkedInUrl: profile.linkedInUrl ?? '',
          logoUrl: profile.logoUrl ?? '',
          foundedYear: profile.foundedYear ?? null,
          city: profile.city ?? '',
          province: profile.province ?? '',
          address: profile.address ?? '',
          postalCode: profile.postalCode ?? '',
          contactEmail: profile.contactEmail ?? '',
          contactPhone: profile.contactPhone ?? ''
        });
        this.createdAt = profile.createdAt ?? null;
        this.updatedAt = profile.updatedAt ?? null;
        this.profileExists = true;
        this.isLoading = false;
      },
      error: error => {
        this.isLoading = false;
        if (error.status === 404) {
          this.profileExists = false;
          this.isEditing = true;
        } else if (error.status === 401) {
          void this.router.navigate(['/login'], { queryParams: { returnUrl: '/employer/profile' } });
        } else {
          this.errorMessage = 'خطا در بارگذاری پروفایل شرکت. لطفاً دوباره تلاش کنید.';
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
      companyName: this.toNullOrTrimmed(value.companyName),
      companyDescription: this.toNullOrTrimmed(value.companyDescription),
      industry: this.toNullOrTrimmed(value.industry),
      companySize: this.toNullOrTrimmed(value.companySize),
      websiteUrl: this.toNullOrTrimmed(value.websiteUrl),
      linkedInUrl: this.toNullOrTrimmed(value.linkedInUrl),
      logoUrl: this.toNullOrTrimmed(value.logoUrl),
      foundedYear: value.foundedYear ?? null,
      city: this.toNullOrTrimmed(value.city),
      province: this.toNullOrTrimmed(value.province),
      address: this.toNullOrTrimmed(value.address),
      postalCode: this.toNullOrTrimmed(value.postalCode),
      contactEmail: this.toNullOrTrimmed(value.contactEmail),
      contactPhone: this.toNullOrTrimmed(value.contactPhone)
    };

    const operation$ = this.profileExists
      ? this.profileService.updateProfile(request)
      : this.profileService.createProfile(request);

    operation$.subscribe({
      next: profile => {
        this.isSaving = false;
        this.profileExists = true;
        this.isEditing = false;
        this.createdAt = profile.createdAt ?? this.createdAt;
        this.updatedAt = profile.updatedAt ?? new Date().toISOString();
        this.successMessage = 'پروفایل شرکت با موفقیت ذخیره شد.';
      },
      error: error => {
        this.isSaving = false;
        if (error.status === 401) {
          void this.router.navigate(['/login'], { queryParams: { returnUrl: '/employer/profile' } });
        } else if (error.status === 409) {
          this.errorMessage = 'پروفایل شرکت قبلاً ایجاد شده است.';
          this.profileExists = true;
        } else if (error.status === 404) {
          this.errorMessage = 'پروفایل شرکت برای ذخیره‌سازی یافت نشد. لطفاً صفحه را دوباره بارگذاری کنید.';
          this.profileExists = false;
        } else if (error.status === 400) {
          this.errorMessage = 'اطلاعات وارد شده معتبر نیست.';
          const errors = error.error?.errors as Record<string, string[]> | undefined;
          if (errors) {
            this.validationErrors = errors;
          }
        } else {
          this.errorMessage = 'ذخیره‌سازی پروفایل شرکت ناموفق بود. لطفاً دوباره تلاش کنید.';
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

  uploadLogo(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const file = input.files[0];
    const formData = new FormData();
    formData.append('file', file);

    this.isUploadingLogo = true;
    this.errorMessage = '';

    this.profileService.uploadLogo(formData).subscribe({
      next: result => {
        this.isUploadingLogo = false;
        this.profileForm.patchValue({ logoUrl: result.logoUrl });
        this.successMessage = 'لوگو با موفقیت آپلود شد.';
      },
      error: () => {
        this.isUploadingLogo = false;
        this.errorMessage = 'آپلود لوگو ناموفق بود. لطفاً با فرمت تصویری (JPEG, PNG, GIF, WebP) و حجم کمتر از ۲ مگابایت سعی کنید.';
      }
    });
  }

  hasError(controlName: string): boolean {
    const control = this.profileForm.get(controlName);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  getValidationMessages(controlName: string): string[] {
    return this.validationErrors[controlName] ?? [];
  }

  formatDate(value: string): string {
    return this.jalaliDate.formatDisplay(value);
  }

  industryLabel(value: string): string {
    return this.industryOptions.find(option => option.value === value)?.label ?? value;
  }

  companySizeLabel(value: string): string {
    return this.companySizeOptions.find(option => option.value === value)?.label ?? value;
  }

  private toNullOrTrimmed(value: string): string | null {
    const trimmed = value?.trim() ?? '';
    return trimmed.length > 0 ? trimmed : null;
  }
}


