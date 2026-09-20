import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Router } from '@angular/router';
import { throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../services/auth.service';
import { AuthResponse, UserProfile } from '../../models/auth.models';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  const mockUser: UserProfile = {
    id: '1', firstName: 'Test', lastName: 'User',
    email: 'test@example.com', phoneNumber: '09123456789', roles: ['Candidate']
  };

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['login']);
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigateByUrl');
    fixture.detectChanges();
  });

  it('should render the login form with Persian labels', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('ورود به ایران‌جاب');
    expect(compiled.textContent).toContain('ایمیل یا شماره موبایل');
    expect(compiled.textContent).toContain('رمز عبور');
    expect(compiled.textContent).toContain('ثبت‌نام');
    expect(compiled.textContent).toContain('فراموشی رمز عبور');
  });

  it('should disable the submit button while the form is invalid', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const button = compiled.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(button.disabled).toBeTrue();
  });

  it('should not call the API when the form is invalid', () => {
    component.loginForm.patchValue({ identifier: '', password: '' });
    component.onSubmit();
    expect(authServiceSpy.login).not.toHaveBeenCalled();
  });

  it('should navigate to /account after a successful login', () => {
    authServiceSpy.login.and.returnValue({
      accessToken: 'token',
      expiresAt: new Date().toISOString(),
      user: mockUser
    } as AuthResponse);

    component.loginForm.patchValue({ identifier: 'test@example.com', password: 'Password123!' });
    component.onSubmit();

    expect(authServiceSpy.login).toHaveBeenCalledWith({
      identifier: 'test@example.com',
      password: 'Password123!'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/account');
    expect(component.isLoading).toBeFalse();
  });

  it('should show a Persian error message when login fails', () => {
    authServiceSpy.login.and.returnValue(throwError(() => new Error('unauthorized')));

    component.loginForm.patchValue({ identifier: 'test@example.com', password: 'wrong' });
    component.onSubmit();

    expect(component.errorMessage).toContain('ورود ناموفق بود');
    expect(component.isLoading).toBeFalse();
  });
});
