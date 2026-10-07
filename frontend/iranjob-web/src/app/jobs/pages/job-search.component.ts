import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, catchError, map, merge, of, switchMap, tap, withLatestFrom } from 'rxjs';
import { JobCategoryService } from '../../reference-data/services/job-category.service';
import { LocationService } from '../../reference-data/services/location.service';
import { SkillService } from '../../reference-data/services/skill.service';
import { JalaliDateService } from '../../core/services/jalali-date.service';
import { City, Country, JobCategory, Province, Skill } from '../../reference-data/models/reference-data.models';
import { PublicJobSearchItem, PublicJobSearchPage, PublicJobSearchQuery, PublicJobSort } from '../models/public-job.models';
import { PublicJobSearchService } from '../services/public-job-search.service';

const EMPTY_PAGE: PublicJobSearchPage = { items: [], page: 1, pageSize: 12, totalCount: 0, totalPages: 0 };

@Component({
  selector: 'app-job-search',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './job-search.component.html',
  styleUrl: './job-search.component.scss'
})
export class JobSearchComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchService = inject(PublicJobSearchService);
  private readonly categoryService = inject(JobCategoryService);
  private readonly skillService = inject(SkillService);
    private readonly locationService = inject(LocationService);
  private readonly jalaliDate = inject(JalaliDateService);
  private readonly retrySearch = new Subject<void>();

  readonly filters = this.fb.group({
    q: [''],
    categoryId: [''],
    skillIds: this.fb.nonNullable.control<string[]>([]),
    countryId: [''],
    provinceId: [''],
    cityId: [''],
    employmentType: [''],
    workArrangement: [''],
    salaryMinimum: [''],
    salaryMaximum: [''],
    salaryCurrency: [''],
    sort: this.fb.nonNullable.control<PublicJobSort>('Newest')
  });

  categories: JobCategory[] = [];
  skills: Skill[] = [];
  countries: Country[] = [];
  provinces: Province[] = [];
  cities: City[] = [];
  results: PublicJobSearchPage = EMPTY_PAGE;
  currentQuery: PublicJobSearchQuery = { page: 1, pageSize: 12, sort: 'Newest' };
  isLoading = true;
  filtersOpen = false;
  hasError = false;
  lookupError = false;
  filterValidationMessage = '';

  ngOnInit(): void {
    this.loadLookups();
    const queryChanges = this.route.queryParamMap.pipe(map(params => this.readQuery(params)));
    const retries = this.retrySearch.pipe(
      withLatestFrom(this.route.queryParamMap),
      map(([, params]) => this.readQuery(params))
    );
    merge(queryChanges, retries).pipe(
      tap(query => {
        this.currentQuery = query;
        this.patchForm(query);
        this.restoreLocationOptions(query);
        this.isLoading = true;
        this.hasError = false;
      }),
      switchMap(query => this.searchService.search(query).pipe(
        map(page => ({ page, error: false })),
        catchError(() => of({ page: EMPTY_PAGE, error: true }))
      )),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(({ page, error }) => {
      this.results = page;
      this.hasError = error;
      this.isLoading = false;
    });
  }

  applyFilters(): void {
    const value = this.filters.getRawValue();
    this.filterValidationMessage = '';
    const hasSalaryRange = value.salaryMinimum !== '' || value.salaryMaximum !== '';
    if ((hasSalaryRange || value.sort.startsWith('Salary')) && !/^[a-z]{3}$/i.test((value.salaryCurrency ?? '').trim())) {
      this.filterValidationMessage = 'برای فیلتر یا مرتب‌سازی حقوق، واحد پول سه‌حرفی را وارد کنید.';
      return;
    }
    if ((value.salaryMinimum !== '' && Number(value.salaryMinimum) < 0)
      || (value.salaryMaximum !== '' && Number(value.salaryMaximum) < 0)
      || (value.salaryMinimum !== '' && value.salaryMaximum !== ''
        && Number(value.salaryMaximum) < Number(value.salaryMinimum))) {
      this.filterValidationMessage = 'محدودهٔ حقوق را بررسی کنید؛ مقادیر باید مثبت باشند و حداکثر از حداقل کمتر نباشد.';
      return;
    }
    const queryParams: Record<string, string | string[] | number | null> = {
      q: (value.q ?? '').trim() || null,
      categoryId: value.categoryId || null,
      skillIds: value.skillIds.length ? value.skillIds : null,
      countryId: value.countryId || null,
      provinceId: value.provinceId || null,
      cityId: value.cityId || null,
      employmentType: value.employmentType || null,
      workArrangement: value.workArrangement || null,
      salaryMinimum: value.salaryMinimum ? Number(value.salaryMinimum) : null,
      salaryMaximum: value.salaryMaximum ? Number(value.salaryMaximum) : null,
      salaryCurrency: (value.salaryCurrency ?? '').trim().toUpperCase() || null,
      sort: value.sort,
      page: 1,
      pageSize: 12
    };
    this.filtersOpen = false;
    void this.router.navigate([], { relativeTo: this.route, queryParams, queryParamsHandling: 'merge' });
  }

  clearFilters(): void {
    this.filters.reset({
      q: '', categoryId: '', skillIds: [], countryId: '', provinceId: '', cityId: '',
      employmentType: '', workArrangement: '', salaryMinimum: '', salaryMaximum: '', salaryCurrency: '', sort: 'Newest'
    });
    this.provinces = [];
    this.cities = [];
    this.filtersOpen = false;
    void this.router.navigate([], { relativeTo: this.route, queryParams: {}, replaceUrl: true });
  }

  changeSort(sort: PublicJobSort): void {
    this.filters.controls.sort.setValue(sort);
    this.applyFilters();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.results.totalPages || page === this.results.page) return;
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page },
      queryParamsHandling: 'merge'
    });
  }

  retry(): void {
    this.retrySearch.next();
  }

  onCountryChange(): void {
    const countryId = this.filters.controls.countryId.value;
    this.filters.patchValue({ provinceId: '', cityId: '' });
    this.provinces = [];
    this.cities = [];
    if (countryId) {
      this.locationService.getProvinces(countryId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: provinces => this.provinces = provinces,
        error: () => this.lookupError = true
      });
    }
  }

  onProvinceChange(): void {
    const provinceId = this.filters.controls.provinceId.value;
    this.filters.controls.cityId.setValue('');
    this.cities = [];
    if (provinceId) {
      this.locationService.getCities(provinceId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: cities => this.cities = cities,
        error: () => this.lookupError = true
      });
    }
  }

  formatSalary(job: PublicJobSearchItem): string | null {
    if (job.salaryMinimum === null && job.salaryMaximum === null) return null;
    const amount = job.salaryMinimum !== null && job.salaryMaximum !== null
      ? `${this.formatNumber(job.salaryMinimum)} تا ${this.formatNumber(job.salaryMaximum)}`
      : this.formatNumber(job.salaryMinimum ?? job.salaryMaximum ?? 0);
    const period = job.salaryPeriod === 'Monthly' ? ' ماهانه' : job.salaryPeriod === 'Yearly' ? ' سالانه' : '';
    return `${amount} ${job.salaryCurrency ?? ''}${period}`.trim();
  }

    arrangementLabel(value: string): string {
    return ({ OnSite: 'حضوری', Hybrid: 'ترکیبی', Remote: 'دورکاری' } as Record<string, string>)[value] ?? value;
  }

  employmentLabel(value: string): string {
    return ({ FullTime: 'تمام‌وقت', PartTime: 'پاره‌وقت', Contract: 'قراردادی', Internship: 'کارآموزی', Temporary: 'موقت' } as Record<string, string>)[value] ?? value;
  }

  private loadLookups(): void {
    this.categoryService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: items => this.categories = items,
      error: () => this.lookupError = true
    });
    this.skillService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: items => this.skills = items,
      error: () => this.lookupError = true
    });
    this.locationService.getCountries().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: items => this.countries = items,
      error: () => this.lookupError = true
    });
  }

  private readQuery(params: import('@angular/router').ParamMap): PublicJobSearchQuery {
    const numberOrUndefined = (value: string | null): number | undefined => {
      if (value === null || value.trim() === '') return undefined;
      const number = Number(value);
      return Number.isFinite(number) ? number : undefined;
    };
    const rawPage = Number(params.get('page'));
    const rawPageSize = Number(params.get('pageSize'));
    const sort = params.get('sort') as PublicJobSort | null;
    const supportedSorts: PublicJobSort[] = ['Newest', 'Oldest', 'SalaryAscending', 'SalaryDescending'];
    return {
      q: params.get('q') || undefined,
      categoryId: params.get('categoryId') || undefined,
      skillIds: params.getAll('skillIds'),
      countryId: params.get('countryId') || undefined,
      provinceId: params.get('provinceId') || undefined,
      cityId: params.get('cityId') || undefined,
      employmentType: params.get('employmentType') || undefined,
      workArrangement: params.get('workArrangement') || undefined,
      salaryMinimum: numberOrUndefined(params.get('salaryMinimum')),
      salaryMaximum: numberOrUndefined(params.get('salaryMaximum')),
      salaryCurrency: params.get('salaryCurrency') || undefined,
      page: Number.isSafeInteger(rawPage) && rawPage > 0 ? rawPage : 1,
      pageSize: Number.isSafeInteger(rawPageSize) && rawPageSize > 0 && rawPageSize <= 100 ? rawPageSize : 12,
      sort: sort && supportedSorts.includes(sort) ? sort : 'Newest'
    };
  }

  private patchForm(query: PublicJobSearchQuery): void {
    this.filters.patchValue({
      q: query.q ?? '', categoryId: query.categoryId ?? '', skillIds: query.skillIds ?? [],
      countryId: query.countryId ?? '', provinceId: query.provinceId ?? '', cityId: query.cityId ?? '',
      employmentType: query.employmentType ?? '', workArrangement: query.workArrangement ?? '',
      salaryMinimum: query.salaryMinimum?.toString() ?? '', salaryMaximum: query.salaryMaximum?.toString() ?? '',
      salaryCurrency: query.salaryCurrency ?? '', sort: query.sort ?? 'Newest'
    });
  }

  private restoreLocationOptions(query: PublicJobSearchQuery): void {
    if (!query.countryId) {
      this.provinces = [];
      this.cities = [];
      return;
    }
    this.locationService.getProvinces(query.countryId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: provinces => {
        this.provinces = provinces;
        if (!query.provinceId) {
          this.cities = [];
          return;
        }
        this.locationService.getCities(query.provinceId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: cities => this.cities = cities,
          error: () => this.lookupError = true
        });
      },
      error: () => this.lookupError = true
    });
    }

  formatDate(value: string): string {
    return this.jalaliDate.formatDisplay(value);
  }

  formatCount(value: number): string {
    return this.jalaliDate.toPersianDigits(value.toString());
  }

  private formatNumber(value: number): string {
    return this.jalaliDate.toPersianDigits(value.toString());
  }
}
