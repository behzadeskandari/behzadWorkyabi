import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AccountComponent } from './account.component';
import { AuthService } from '../../services/auth.service';
import { UserProfile } from '../../models/auth.models';

describe('AccountComponent', () => {
  let component: AccountComponent;
  let fixture: ComponentFixture<AccountComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  const mockUser: UserProfile = {
    id: '1', firstName: 'بهزاد', lastName: 'اسکندری',
    email: 'behzad@example.com', phoneNumber: '09123456789', roles: ['Candidate']
  };

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['getCurrentUser', 'logout']);
    await TestBed.configureTestingModule({
      imports: [AccountComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AccountComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
  });

  it('should display the user profile information and roles', () => {
    authServiceSpy.getCurrentUser.and.returnValue(of(mockUser));

    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('بهزاد');
    expect(compiled.textContent).toContain('اسکندری');
    expect(compiled.textContent).toContain('behzad@example.com');
    expect(compiled.textContent).toContain('09123456789');
    expect(compiled.textContent).toContain('کارجو');
    expect(component.isLoading).toBeFalse();
  });

  it('should translate known role names to Persian', () => {
    expect(component.getRoleName('Candidate')).toBe('کارجو');
    expect(component.getRoleName('Employer')).toBe('کارفرما');
    expect(component.getRoleName('Admin')).toBe('مدیر');
    expect(component.getRoleName('SuperAdmin')).toBe('مدیر ارشد');
    expect(component.getRoleName('UnknownRole')).toBe('UnknownRole');
  });

  it('should redirect to /login when the profile request returns 401', () => {
    authServiceSpy.getCurrentUser.and.returnValue(
      throwError(() => ({ status: 401 }))
    );

    fixture.detectChanges();

    expect(router.navigate).toHaveBeenCalledWith(['/login']);
    expect(component.isLoading).toBeFalse();
  });

  it('should show an error message for other profile failures', () => {
    authServiceSpy.getCurrentUser.and.returnValue(
      throwError(() => ({ status: 500 }))
    );

    fixture.detectChanges();

    expect(component.errorMessage).toContain('خطا در بارگذاری');
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('should logout, clear state and navigate to /login on success', () => {
    authServiceSpy.getCurrentUser.and.returnValue(of(mockUser));
    authServiceSpy.logout.and.returnValue(of(undefined));

    fixture.detectChanges();
    component.logout();

    expect(authServiceSpy.logout).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should navigate to /login even when the backend logout call fails', () => {
    authServiceSpy.getCurrentUser.and.returnValue(of(mockUser));
    authServiceSpy.logout.and.returnValue(
      throwError(() => new Error('network error'))
    );

    fixture.detectChanges();
    component.logout();

    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });
});
