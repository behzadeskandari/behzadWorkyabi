import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Router } from '@angular/router';
import { throwError } from 'rxjs';
import { RegisterComponent } from './register.component';
import { AuthService } from '../../services/auth.service';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['register']);
    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
    fixture.detectChanges();
  });

  it('should render the registration form with Persian labels', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('ثبت‌نام در ایران‌جاب');
    expect(compiled.textContent).toContain('نام خانوادگی');
    expect(compiled.textContent).toContain('شماره موبایل');
    expect(compiled.textContent).toContain('نوع حساب');
  });

  it('should only offer Candidate and Employer account types', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const options = Array.from(compiled.querySelectorAll('select#role option'));
    const values = options.map(option => option.value);

    expect(values).toEqual(['Candidate', 'Employer']);
    expect(values).not.toContain('Admin');
    expect(values).not.toContain('SuperAdmin');
    expect(values).not.toContain('Recruiter');
  });

  it('should reject an invalid Iranian mobile number', () => {
    component.registerForm.patchValue({
      firstName: 'Behzad',
      lastName: 'Eskandari',
      email: 'behzad@example.com',
      phoneNumber: '12345',
      password: 'Password123!',
      confirmPassword: 'Password123!'
    });

    expect(component.registerForm.get('phoneNumber')?.invalid).toBeTrue();
    expect(component.registerForm.invalid).toBeTrue();

    component.onSubmit();
    expect(authServiceSpy.register).not.toHaveBeenCalled();
  });

  it('should accept a valid Iranian mobile number', () => {
    component.registerForm.patchValue({
      firstName: 'Behzad',
      lastName: 'Eskandari',
      email: 'behzad@example.com',
      phoneNumber: '09123456789',
      password: 'Password123!',
      confirmPassword: 'Password123!'
    });

    expect(component.registerForm.get('phoneNumber')?.valid).toBeTrue();
    expect(component.registerForm.valid).toBeTrue();
  });

  it('should reject mismatched password confirmation', () => {
    component.registerForm.patchValue({
      firstName: 'Behzad',
      lastName: 'Eskandari',
      email: 'behzad@example.com',
      phoneNumber: '09123456789',
      password: 'Password123!',
      confirmPassword: 'Different123!'
    });

    expect(component.registerForm.errors?.['passwordMismatch']).toBeTrue();
  });

  it('should reject a weak password', () => {
    component.registerForm.patchValue({
      firstName: 'Behzad',
      lastName: 'Eskandari',
      email: 'behzad@example.com',
      phoneNumber: '09123456789',
      password: 'weakpass',
      confirmPassword: 'weakpass'
    });

    expect(component.registerForm.get('password')?.invalid).toBeTrue();
  });

  it('should submit a valid Candidate registration and navigate to /login', () => {
    authServiceSpy.register.and.returnValue(undefined as never);

    component.registerForm.patchValue({
      firstName: 'Behzad',
      lastName: 'Eskandari',
      email: 'behzad@example.com',
      phoneNumber: '09123456789',
      password: 'Password123!',
      confirmPassword: 'Password123!',
      role: 'Candidate'
    });

    component.onSubmit();

    expect(authServiceSpy.register).toHaveBeenCalledWith({
      firstName: 'Behzad',
      lastName: 'Eskandari',
      email: 'behzad@example.com',
      phoneNumber: '09123456789',
      password: 'Password123!',
      role: 'Candidate'
    });
    expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { registered: true } });
  });

  it('should show a duplicate-account message when the API returns 409', () => {
    authServiceSpy.register.and.returnValue(
      throwError(() => ({ status: 409 }))
    );

    component.registerForm.patchValue({
      firstName: 'Behzad',
      lastName: 'Eskandari',
      email: 'taken@example.com',
      phoneNumber: '09123456789',
      password: 'Password123!',
      confirmPassword: 'Password123!'
    });

    component.onSubmit();

    expect(component.errorMessage).toContain('قبلاً ثبت شده است');
    expect(component.isLoading).toBeFalse();
  });

  it('should show an invalid-data message when the API returns 400', () => {
    authServiceSpy.register.and.returnValue(
      throwError(() => ({ status: 400 }))
    );

    component.registerForm.patchValue({
      firstName: 'Behzad',
      lastName: 'Eskandari',
      email: 'behzad@example.com',
      phoneNumber: '09123456789',
      password: 'Password123!',
      confirmPassword: 'Password123!'
    });

    component.onSubmit();

    expect(component.errorMessage).toContain('معتبر نیست');
  });
});
