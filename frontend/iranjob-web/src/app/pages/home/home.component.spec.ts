import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { HomeComponent } from './home.component';
import { PublicJobSearchService } from '../../jobs/services/public-job-search.service';
import { PublicJobSearchPage } from '../../jobs/models/public-job.models';
import { AuthService } from '../../auth/services/auth.service';
import { provideRouter } from '@angular/router';

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;

  const EMPTY_PAGE: PublicJobSearchPage = { items: [], page: 1, pageSize: 12, totalCount: 0, totalPages: 0 };

  beforeEach(async () => {
    // The home page now loads featured published jobs through
    // PublicJobSearchService (HttpClient-backed) and gates CTAs on AuthService,
    // so both are stubbed to keep the spec hermetic.
    const search = jasmine.createSpyObj<PublicJobSearchService>('PublicJobSearchService', ['search']);
    search.search.and.returnValue(of(EMPTY_PAGE));
    const auth = jasmine.createSpyObj<AuthService>('AuthService', ['hasRole', 'isAuthenticated']);
    auth.hasRole.and.returnValue(false);
    auth.isAuthenticated.and.returnValue(false);

    await TestBed.configureTestingModule({
      imports: [HomeComponent],
      providers: [
        provideRouter([]),
        { provide: PublicJobSearchService, useValue: search },
        { provide: AuthService, useValue: auth }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should display IranJob title', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain(component.title);
  });

  it('should display welcome message', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.welcome')?.textContent).toContain(component.welcomeMessage);
  });

  it('should render the featured jobs section in loading-free empty state', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(component.isLoading).toBeFalse();
    expect(compiled.querySelector('.empty-state')).not.toBeNull();
  });
});
