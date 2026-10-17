import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { By } from '@angular/platform-browser';
import { ReferenceDataManagementComponent } from './reference-data.component';

const API = 'http://localhost:5158/api/v1';

describe('ReferenceDataManagementComponent', () => {
  let fixture: ComponentFixture<ReferenceDataManagementComponent>;
  let httpMock: HttpTestingController;
  let component: ReferenceDataManagementComponent;

  const mockCountries = [
    { id: 'c1', name: 'ایران', code: 'IR', isActive: true, createdAt: '2026-01-01T00:00:00Z', updatedAt: null },
  ];

  const mockCategories = [
    { id: 'cat1', name: 'فناوری اطلاعات', description: 'IT roles', isActive: true, createdAt: '2026-01-01T00:00:00Z', updatedAt: null },
  ];

  const mockSkills = [
    { id: 'sk1', name: 'انگولار', description: null, isActive: true, createdAt: '2026-01-01T00:00:00Z', updatedAt: null },
  ];

  function flushInitialData(): void {
    httpMock.expectOne(`${API}/locations/countries`).flush(mockCountries);
    httpMock.expectOne(`${API}/job-categories`).flush(mockCategories);
    httpMock.expectOne(`${API}/skills`).flush(mockSkills);
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReferenceDataManagementComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ReferenceDataManagementComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => httpMock.verify());

  it('should show loading state initially on countries tab', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('در حال بارگذاری کشورها');
    flushInitialData();
  });

  it('should render countries table after loading', () => {
    fixture.detectChanges();
    flushInitialData();
    fixture.detectChanges();

    const rows = fixture.debugElement.queryAll(By.css('.ref-data__table tbody tr'));
    expect(rows.length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('ایران');
    expect(fixture.nativeElement.textContent).toContain('IR');
    expect(fixture.nativeElement.textContent).toContain('فعال');
  });

  it('should render empty state when no countries', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${API}/locations/countries`).flush([]);
    httpMock.expectOne(`${API}/job-categories`).flush(mockCategories);
    httpMock.expectOne(`${API}/skills`).flush(mockSkills);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('هیچ کشوری یافت نشد');
  });

  it('should switch to categories tab and display them', () => {
    fixture.detectChanges();
    flushInitialData();
    fixture.detectChanges();
    component.switchTab('categories');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('فناوری اطلاعات');
  });

  it('should switch to skills tab and display them', () => {
    fixture.detectChanges();
    flushInitialData();
    fixture.detectChanges();
    component.switchTab('skills');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('انگولار');
  });

  it('should open and close the country create form', () => {
    fixture.detectChanges();
    flushInitialData();
    fixture.detectChanges();
    component.startCreateCountry();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('کشور جدید');
    component.cancelForm();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('کشور جدید');
  });

  it('should validate required fields on country create', () => {
    fixture.detectChanges();
    flushInitialData();
    fixture.detectChanges();
    component.startCreateCountry();
    component.countryForm = { name: '', code: '' };
    component.submitCountry();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('نام و کد کشور الزامی هستند');
    httpMock.expectNone(`${API}/locations/countries`);
  });

  it('should create a country on valid submit', () => {
    fixture.detectChanges();
    flushInitialData();
    fixture.detectChanges();

    component.startCreateCountry();
    component.countryForm = { name: 'آلمان', code: 'DE' };
    component.submitCountry();

    const req = httpMock.expectOne(`${API}/locations/countries`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ name: 'آلمان', code: 'DE' });
    req.flush({ id: 'c3', name: 'آلمان', code: 'DE', isActive: true, createdAt: '', updatedAt: null });

    // Reload triggered by success callback
    httpMock.expectOne(`${API}/locations/countries`).flush([...mockCountries, { id: 'c3', name: 'آلمان', code: 'DE', isActive: true, createdAt: '', updatedAt: null }]);
    fixture.detectChanges();
    expect(component.isFormOpen).toBeFalse();
  });

  it('should show server validation error on duplicate country code', () => {
    fixture.detectChanges();
    flushInitialData();
    fixture.detectChanges();

    component.startCreateCountry();
    component.countryForm = { name: 'Duplicate', code: 'IR' };
    component.submitCountry();

    const req = httpMock.expectOne(`${API}/locations/countries`);
    req.flush({ message: 'Conflict' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('کد کشور تکراری است');
  });

  it('should deactivate a country with confirmation', () => {
    fixture.detectChanges();
    flushInitialData();
    fixture.detectChanges();

    spyOn(window, 'confirm').and.returnValue(true);
    component.deactivateCountry(mockCountries[0]);
    const req = httpMock.expectOne(`${API}/locations/countries/c1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
    // Reload triggered by success callback
    httpMock.expectOne(`${API}/locations/countries`).flush(mockCountries);
  });

  it('should not call delete when confirm is cancelled', () => {
    fixture.detectChanges();
    flushInitialData();
    fixture.detectChanges();

    spyOn(window, 'confirm').and.returnValue(false);
    component.deactivateCountry(mockCountries[0]);
    httpMock.expectNone(`${API}/locations/countries/c1`);
  });

  it('should load provinces when country filter changes', () => {
    fixture.detectChanges();
    flushInitialData();
    fixture.detectChanges();

    component.switchTab('provinces');
    fixture.detectChanges();
    component.onCountryFilterChange({ target: { value: 'c1' } } as unknown as Event);
    const req = httpMock.expectOne(`${API}/locations/provinces?countryId=c1`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});

