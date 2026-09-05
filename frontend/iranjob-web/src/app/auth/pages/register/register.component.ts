import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly registerForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^09\d{9}$/)]],
    password: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$/)
    ]],
    confirmPassword: ['', [Validators.required]],
    role: ['Candidate', [Validators.required]]
  }, { validators: passwordMatchValidator });

  isLoading = false;
  errorMessage = '';

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    const value = this.registerForm.getRawValue();

    this.authService.register({
      firstName: value.firstName,
      lastName: value.lastName,
      email: value.email,
      phoneNumber: value.phoneNumber,
      password: value.password,
      role: value.role as 'Candidate' | 'Employer'
    }).subscribe({
      next: () => {
        this.isLoading = false;
        void this.router.navigate(['/login'], { queryParams: { registered: true } });
      },
      error: error => {
        this.isLoading = false;
        if (error.status === 409) {
          this.errorMessage = 'این ایمیل یا شماره موبایل قبلاً ثبت شده است.';
        } else if (error.status === 400) {
          this.errorMessage = 'اطلاعات وارد شده معتبر نیست.';
        } else {
          this.errorMessage = 'ثبت‌نام ناموفق بود. لطفاً دوباره تلاش کنید.';
        }
      }
    });
  }
}

function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  return password === confirmPassword ? null : { passwordMismatch: true };
}
