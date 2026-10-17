import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { AdminService } from '../../services/admin.service';
import { AdminReferenceDataService } from '../../services/admin-reference-data.service';
import { JobCategoryService } from '../../../reference-data/services/job-category.service';
import { JalaliDateService } from '../../../core/services/jalali-date.service';
import {
  AdminJobListItem,
  AdminJobPageResult,
  JobModerationStatus,
} from '../../models/admin.models';
import { Country, Province, City, JobCategory } from '../../../reference-data/models/reference-data.models';

@Component({
  selector: 'app-job-moderation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './job-moderation.component.html',
  styleUrls: ['./job-moderation.component.scss'],
})
export class JobModerationComponent implements OnInit, OnDestroy {
  private readonly adminService = inject(AdminService);
  private readonly refDataService = inject(AdminReferenceDataService);
  private readonly categoryService = inject(JobCategoryService);
  private readonly jalaliDate = inject(JalaliDateService);

  protected readonly Math = Math;

  // ── Data state ──

  jobs = signal<AdminJobListItem[]>([]);
  totalCount = signal<number>(0);
  currentPage = signal<number>(1);
  pageSize = signal<number>(20);
  totalPages = signal<number>(1);
  isLoading = signal<boolean>(true);
  error = signal<string | null>(null);

  // ── Filter state ──

  searchTerm = signal<string>('');
  selectedStatus = signal<JobModerationStatus | ''>('');
  selectedCategoryId = signal<string>('');
  selectedCountryId = signal<string>('');
  selectedProvinceId = signal<string>('');
  selectedCityId = signal<string>('');
  employerName = signal<string>('');
  sort = signal<string>('Newest');

  // ── Lookup data ──

  categories = signal<JobCategory[]>([]);
  countries = signal<Country[]>([]);
  provinces = signal<Province[]>([]);
  cities = signal<City[]>([]);
  isLookupsLoading = signal<boolean>(true);

  // ── Search debounce ──

  private readonly searchSubject = new Subject<string>();
  private readonly searchDebounceMs = 500;

  // ── Constants ──

  readonly pageSizeOptions = [10, 20, 50];
  readonly sortOptions: Array<{ value: string; label: string }> = [
    { value: 'Newest', label: 'جدیدترین' },
    { value: 'Oldest', label: 'قدیمی‌ترین' },
    { value: 'TitleAscending', label: 'عنوان (الف→ی)' },
    { value: 'TitleDescending', label: 'عنوان (ی→الف)' },
  ];
  readonly statusOptions: Array<{ value: JobModerationStatus; label: string }> = [
    { value: 'Draft', label: 'پیش‌نویس' },
    { value: 'Published', label: 'منتشر شده' },
    { value: 'Closed', label: 'بسته شده' },
  ];

  readonly employmentTypeLabels: Record<string, string> = {
    FullTime: 'تمام‌وقت',
    PartTime: 'پاره‌وقت',
    Contract: 'قراردادی',
    Internship: 'کارآموزی',
    Temporary: 'موقت',
  };

  readonly workArrangementLabels: Record<string, string> = {
    OnSite: 'حضوری',
    Hybrid: 'ترکیبی',
    Remote: 'دورکاری',
  };

  // ── Lifecycle ──

  ngOnInit(): void {
    this.loadLookups();
    this.loadJobs();
    this.setupSearchDebounce();
  }

  ngOnDestroy(): void {
    this.searchSubject.complete();
  }

  // ── Data loading ──

  /** Load reference data for filter dropdowns. */
  loadLookups(): void {
    this.isLookupsLoading.set(true);

    this.categoryService.getAll().subscribe({
      next: (data) => {
        this.categories.set(data.filter((c) => c.isActive));
        this.isLookupsLoading.set(false);
      },
      error: () => {
        this.categories.set([]);
        this.isLookupsLoading.set(false);
      },
    });

    this.refDataService.getAllCountries().subscribe({
      next: (data) => this.countries.set(data.filter((c) => c.isActive)),
      error: () => this.countries.set([]),
    });
  }

  /** Fetch the paginated, filtered job list from the backend. */
  loadJobs(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.adminService
      .getModeratedJobs(
        this.currentPage(),
        this.pageSize(),
        this.searchTerm() || undefined,
        this.selectedStatus() || undefined,
        this.selectedCategoryId() || undefined,
        this.selectedCityId() || undefined,
        this.employerName() || undefined,
        this.sort()
      )
      .subscribe({
        next: (result: AdminJobPageResult) => {
          this.jobs.set(result.items);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(result.totalPages);
          this.isLoading.set(false);
        },
        error: (err) => {
          if (err.status === 401) {
            this.error.set('دسترسی شما منقضی شده است. لطفاً مجدداً وارد شوید.');
          } else if (err.status === 403) {
            this.error.set('شما دسترسی مدیریت آگهی‌ها ندارید.');
          } else {
            this.error.set('خطا در بارگذاری آگهی‌ها. لطفاً دوباره تلاش کنید.');
          }
          this.jobs.set([]);
          this.totalCount.set(0);
          this.totalPages.set(1);
          this.isLoading.set(false);
        },
      });
  }

  /** Debounced search — waits for the user to stop typing. */
  private setupSearchDebounce(): void {
    this.searchSubject
      .pipe(debounceTime(this.searchDebounceMs), distinctUntilChanged())
      .subscribe(() => {
        this.currentPage.set(1);
        this.loadJobs();
      });
  }

  // ── Filter handlers ──

  onSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchTerm.set(input.value);
    this.searchSubject.next(input.value);
  }

  onEmployerNameInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.employerName.set(input.value);
  }

  onFilterChange(): void {
    this.currentPage.set(1);
    this.loadJobs();
  }

  onStatusChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedStatus.set(select.value as JobModerationStatus | '');
    this.onFilterChange();
  }

  onCategoryChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedCategoryId.set(select.value);
    this.onFilterChange();
  }

  onCountryChangeHandler(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedCountryId.set(select.value);
    this.onCountryChange();
  }

  onProvinceChangeHandler(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedProvinceId.set(select.value);
    this.onProvinceChange();
  }

  onCityChangeHandler(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedCityId.set(select.value);
    this.onFilterChange();
  }

  onCountryChange(): void {
    this.selectedProvinceId.set('');
    this.selectedCityId.set('');
    this.provinces.set([]);
    this.cities.set([]);

    if (this.selectedCountryId()) {
      this.refDataService.getAllProvinces(this.selectedCountryId()).subscribe({
        next: (data) => this.provinces.set(data.filter((p) => p.isActive)),
        error: () => this.provinces.set([]),
      });
    }
  }

  onProvinceChange(): void {
    this.selectedCityId.set('');
    this.cities.set([]);

    if (this.selectedProvinceId()) {
      this.refDataService.getCitiesByProvince(this.selectedProvinceId()).subscribe({
        next: (data) => this.cities.set(data.filter((c) => c.isActive)),
        error: () => this.cities.set([]),
      });
    }
  }

  onPageSizeChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.pageSize.set(Number(select.value));
    this.currentPage.set(1);
    this.loadJobs();
  }

  onSortChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.sort.set(select.value);
    this.loadJobs();
  }

  resetFilters(): void {
    this.searchTerm.set('');
    this.selectedStatus.set('');
    this.selectedCategoryId.set('');
    this.selectedCountryId.set('');
    this.selectedProvinceId.set('');
    this.selectedCityId.set('');
    this.provinces.set([]);
    this.cities.set([]);
    this.employerName.set('');
    this.sort.set('Newest');
    this.currentPage.set(1);
    this.loadJobs();
  }

  // ── Pagination ──

  goToPage(page: number): void {
    const maxPage = this.totalPages();
    this.currentPage.set(Math.min(Math.max(1, page), maxPage));
    this.loadJobs();
  }

  /** Refresh the current page + filters. */
  refresh(): void {
    this.loadJobs();
  }

  // ── Moderation actions ──

  /** Unpublish a published job posting. */
  moderateUnpublish(job: AdminJobListItem): void {
    if (job.status !== 'Published') return;

    const confirmed = window.confirm(
      'آیا مطمئن هستید که می‌خواهید آگهی "' + job.title + '" را از انتشار خارج کنید؟'
    );
    if (!confirmed) return;

    this.adminService
      .moderateUnpublish(job.id, { reason: null })
      .subscribe({
        next: () => this.refresh(),
        error: (err) => {
          if (err.status === 409) {
            this.error.set('این عملیات با وضعیت فعلی آگهی سازگار نیست.');
          } else {
            this.error.set('خطا در انجام عملیات. لطفاً دوباره تلاش کنید.');
          }
        },
      });
  }

  /** Publish a draft job posting. */
  moderatePublish(job: AdminJobListItem): void {
    if (job.status !== 'Draft') return;

    const confirmed = window.confirm(
      'آیا مطمئن هستید که می‌خواهید آگهی "' + job.title + '" را منتشر کنید؟'
    );
    if (!confirmed) return;

    this.adminService
      .moderatePublish(job.id, { reason: null })
      .subscribe({
        next: () => this.refresh(),
        error: (err) => {
          if (err.status === 409) {
            this.error.set('این عملیات با وضعیت فعلی آگهی سازگار نیست.');
          } else {
            this.error.set('خطا در انجام عملیات. لطفاً دوباره تلاش کنید.');
          }
        },
      });
  }

  /** Close a draft or published job posting. */
  moderateClose(job: AdminJobListItem): void {
    if (job.status === 'Closed') return;

    const confirmed = window.confirm(
      'آیا مطمئن هستید که می‌خواهید آگهی "' + job.title + '" را ببندید؟\n\nاین عملیات قابل‌عکس‌نگاری است.'
    );
    if (!confirmed) return;

    this.adminService
      .moderateClose(job.id, { reason: null })
      .subscribe({
        next: () => this.refresh(),
        error: (err) => {
          if (err.status === 409) {
            this.error.set('این عملیات با وضعیت فعلی آگهی سازگار نیست.');
          } else {
            this.error.set('خطا در انجام عملیات. لطفاً دوباره تلاش کنید.');
          }
        },
      });
  }

  // ── Display helpers ──

  getStatusLabel(status: string): string {
    const labels: Record<string, string> = {
      Draft: 'پیش‌نویس',
      Published: 'منتشر شده',
      Closed: 'بسته شده',
    };
    return labels[status] ?? status;
  }

  getEmploymentTypeLabel(type: string): string {
    return this.employmentTypeLabels[type] ?? type;
  }

  getWorkArrangementLabel(arrangement: string): string {
    return this.workArrangementLabels[arrangement] ?? arrangement;
  }

  formatDate(dateString: string | null): string {
    if (!dateString) return '-';
    try {
      return this.jalaliDate.formatDisplay(dateString);
    } catch {
      return '-';
    }
  }

  formatNumber(value: number | null): string {
    if (value === null || value === undefined) return '-';
    return this.jalaliDate.toPersianDigits(value.toString());
  }

  canUnpublish(job: AdminJobListItem): boolean {
    return job.status === 'Published';
  }

  canPublish(job: AdminJobListItem): boolean {
    return job.status === 'Draft';
  }

  canClose(job: AdminJobListItem): boolean {
    return job.status === 'Draft' || job.status === 'Published';
  }

  /** Full location string (city, province, country) or dash. */
  getLocation(job: AdminJobListItem): string {
    const parts = [job.cityName, job.provinceName, job.countryName].filter(
      (p) => p != null && p !== ''
    );
    return parts.length > 0 ? parts.join('، ') : '-';
  }
}