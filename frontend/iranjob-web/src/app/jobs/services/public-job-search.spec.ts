import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router, UrlTree } from '@angular/router';
import { BehaviorSubject, EMPTY } from 'rxjs';
import { JobDetailsComponent } from '../pages/job-details.component';
import { JobSearchComponent } from '../pages/job-search.component';
import { PublicJobDetails, PublicJobSearchPage } from '../models/public-job.models';
import { PublicJobSearchService } from './public-job-search.service';

const API = 'http://localhost:5158/api/v1';
const emptyPage: PublicJobSearchPage = { items: [], page: 1, pageSize: 12, totalCount: 0, totalPages: 0 };

describe('PublicJobSearchService', () => {
  let service: PublicJobSearchService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(PublicJobSearchService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends search filters, repeated skills, pagination, and sort as query parameters', () => {
    service.search({ q: 'engineer', categoryId: 'category-1', skillIds: ['skill-1', 'skill-2'], countryId: 'country-1',
      provinceId: 'province-1', cityId: 'city-1', employmentType: 'FullTime', workArrangement: 'Remote',
      salaryMinimum: 100, salaryMaximum: 500, salaryCurrency: 'IRR', page: 2, pageSize: 20, sort: 'SalaryAscending' }).subscribe();
    const request = http.expectOne(req => req.url === `${API}/jobs`);
    expect(request.request.params.get('q')).toBe('engineer');
    expect(request.request.params.get('categoryId')).toBe('category-1');
    expect(request.request.params.getAll('skillIds')).toEqual(['skill-1', 'skill-2']);
    expect(request.request.params.get('cityId')).toBe('city-1');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('sort')).toBe('SalaryAscending');
    request.flush(emptyPage);
  });

  it('loads public job details by an encoded id', () => {
    service.getById('job/42').subscribe();
    const request = http.expectOne(`${API}/jobs/job%2F42`);
    expect(request.request.method).toBe('GET');
    request.flush({} as PublicJobDetails);
  });
});

describe('JobSearchComponent', () => {
  let fixture: ComponentFixture<JobSearchComponent>;
  let http: HttpTestingController;
  let routeParams: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  let router: jasmine.SpyObj<Router>;

  function startSearch(query: Record<string, string | string[]> = {}): void {
    routeParams = new BehaviorSubject(convertToParamMap(query));
    router = jasmine.createSpyObj<Router>('Router', ['navigate', 'createUrlTree', 'serializeUrl'], { events: EMPTY });
    router.navigate.and.resolveTo(true);
    router.createUrlTree.and.returnValue({} as UrlTree);
    router.serializeUrl.and.returnValue('/jobs/job-1');
    TestBed.configureTestingModule({
      imports: [JobSearchComponent],
      providers: [
        provideHttpClient(), provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { queryParamMap: routeParams, snapshot: { queryParamMap: routeParams.value } } },
        { provide: Router, useValue: router }
      ]
    });
    fixture = TestBed.createComponent(JobSearchComponent);
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  }

  function flushLookups(): void {
    http.expectOne(`${API}/job-categories`).flush([]);
    http.expectOne(`${API}/skills`).flush([]);
    http.expectOne(`${API}/locations/countries`).flush([]);
  }

  afterEach(() => {
    http?.verify();
    TestBed.resetTestingModule();
  });

  it('shows a loading state, then the empty state returned by the search API', () => {
    startSearch();
    expect(fixture.componentInstance.isLoading).toBeTrue();
    flushLookups();
    http.expectOne(req => req.url === `${API}/jobs`).flush(emptyPage);
    fixture.detectChanges();
    expect(fixture.componentInstance.isLoading).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('فرصتی پیدا نشد');
  });

  it('keeps selected filters in router query state and resets to page one', () => {
    startSearch({ page: '3' });
    flushLookups();
    http.expectOne(req => req.url === `${API}/jobs`).flush({ ...emptyPage, page: 3, totalPages: 3 });
    fixture.componentInstance.filters.patchValue({ q: 'مهندس', categoryId: 'cat', skillIds: ['s1', 's2'], countryId: 'co', page: 1 } as never);
    fixture.componentInstance.applyFilters();
    expect(router.navigate).toHaveBeenCalledWith([], jasmine.objectContaining({
      queryParams: jasmine.objectContaining({ q: 'مهندس', categoryId: 'cat', skillIds: ['s1', 's2'], countryId: 'co', page: 1 }),
      queryParamsHandling: 'merge'
    }));
  });

  it('loads provinces and cities only after their parent location is selected', () => {
    startSearch();
    flushLookups();
    http.expectOne(req => req.url === `${API}/jobs`).flush(emptyPage);
    fixture.componentInstance.filters.controls.countryId.setValue('country-1');
    fixture.componentInstance.onCountryChange();
    const provinces = http.expectOne(req => req.url === `${API}/locations/provinces` && req.params.get('countryId') === 'country-1');
    provinces.flush([{ id: 'province-1', countryId: 'country-1', name: 'استان' }]);
    expect(fixture.componentInstance.provinces.length).toBe(1);
    fixture.componentInstance.filters.controls.provinceId.setValue('province-1');
    fixture.componentInstance.onProvinceChange();
    const cities = http.expectOne(req => req.url === `${API}/locations/cities` && req.params.get('provinceId') === 'province-1');
    cities.flush([{ id: 'city-1', provinceId: 'province-1', name: 'شهر' }]);
    expect(fixture.componentInstance.cities.length).toBe(1);
  });

  it('clears filters and applies the selected sort through URL state', () => {
    startSearch({ q: 'previous' });
    flushLookups();
    http.expectOne(req => req.url === `${API}/jobs`).flush(emptyPage);
    fixture.componentInstance.filters.patchValue({ q: 'مهندس', salaryCurrency: 'IRR' });
    fixture.componentInstance.changeSort('SalaryDescending');
    expect(router.navigate).toHaveBeenCalledWith([], jasmine.objectContaining({
      queryParams: jasmine.objectContaining({ sort: 'SalaryDescending', salaryCurrency: 'IRR', page: 1 })
    }));
    fixture.componentInstance.clearFilters();
    expect(fixture.componentInstance.filters.controls.q.value).toBe('');
    expect(fixture.componentInstance.filters.controls.sort.value).toBe('Newest');
    expect(router.navigate).toHaveBeenCalledWith([], jasmine.objectContaining({ queryParams: {}, replaceUrl: true }));
  });

  it('renders a keyboard-accessible result link to the details route', () => {
    startSearch();
    flushLookups();
    http.expectOne(req => req.url === `${API}/jobs`).flush({
      ...emptyPage,
      items: [{
        id: 'job-1', title: 'فرصت توسعه‌دهنده', companyName: 'شرکت نمونه', companyLogoUrl: null,
        category: 'فناوری', description: 'شرح کوتاه', skills: ['Angular'], city: 'تهران', province: 'تهران', country: 'ایران',
        employmentType: 'FullTime', workArrangement: 'Remote', salaryMinimum: null, salaryMaximum: null,
        salaryCurrency: null, salaryPeriod: null, publishedAt: '2026-09-01T00:00:00Z'
      }], totalCount: 1, totalPages: 1
    });
    fixture.detectChanges();
    const link: HTMLAnchorElement = fixture.nativeElement.querySelector('.job-card');
    expect(link).not.toBeNull();
    expect(link.getAttribute('href')).toBe('/jobs/job-1');
    expect(link.getAttribute('aria-label')).toContain('فرصت توسعه‌دهنده');
  });

  it('updates pagination in URL state and exposes Persian API errors', () => {
    startSearch();
    flushLookups();
    http.expectOne(req => req.url === `${API}/jobs`).flush({ ...emptyPage, totalPages: 2 });
    fixture.componentInstance.goToPage(2);
    expect(router.navigate).toHaveBeenCalledWith([], jasmine.objectContaining({ queryParams: { page: 2 } }));
    routeParams.next(convertToParamMap({ page: '2' }));
    http.expectOne(req => req.url === `${API}/jobs`).flush('network failure', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(fixture.componentInstance.hasError).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('دریافت فرصت‌های شغلی انجام نشد');
  });
});

describe('JobDetailsComponent', () => {
  let fixture: ComponentFixture<JobDetailsComponent>;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [JobDetailsComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'job-1' }) } } }]
    });
    fixture = TestBed.createComponent(JobDetailsComponent);
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('loads and renders the published details with the anonymous application action', () => {
    expect(fixture.componentInstance.isLoading).toBeTrue();
    const job: PublicJobDetails = {
      id: 'job-1', title: 'مهندس نرم‌افزار', description: 'شرح فرصت', category: 'فناوری', skills: ['Angular'],
      city: 'تهران', province: 'تهران', country: 'ایران', employmentType: 'FullTime', workArrangement: 'Hybrid',
      salaryMinimum: null, salaryMaximum: null, salaryCurrency: null, salaryPeriod: null, publishedAt: '2026-09-01T00:00:00Z',
      employer: { companyName: 'شرکت نمونه', description: null, industry: 'فناوری', websiteUrl: null, logoUrl: null }
    };
    http.expectOne(`${API}/jobs/job-1`).flush(job);
    fixture.detectChanges();
    expect(fixture.componentInstance.isLoading).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('مهندس نرم‌افزار');
    expect(fixture.nativeElement.textContent).toContain('شرکت نمونه');
    expect(fixture.nativeElement.textContent).toContain('درخواست همکاری');
  });
});
