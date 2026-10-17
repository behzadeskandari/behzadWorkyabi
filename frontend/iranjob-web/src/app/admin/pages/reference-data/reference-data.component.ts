import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminReferenceDataService } from '../../services/admin-reference-data.service';
import { Country, Province, City, JobCategory, Skill } from '../../../reference-data/models/reference-data.models';

type EntityType = 'countries' | 'provinces' | 'cities' | 'categories' | 'skills';

interface CountryForm {
  name: string;
  code: string;
}

interface ProvinceForm {
  countryId: string;
  name: string;
}

interface CityForm {
  countryId: string;
  provinceId: string;
  name: string;
}

interface NamedForm {
  name: string;
  description: string | null;
}

@Component({
  selector: 'app-reference-data-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reference-data.component.html',
  styleUrls: ['./reference-data.component.scss'],
})
export class ReferenceDataManagementComponent implements OnInit {
  private readonly refService = inject(AdminReferenceDataService);

  protected readonly Math = Math;

  // Tab navigation
  activeTab = signal<EntityType>('countries');

  // Country list
  countries = signal<Country[]>([]);
  isLoadingCountries = signal<boolean>(true);

  // Province list (dependent on country)
  provinces = signal<Province[]>([]);
  isLoadingProvinces = signal<boolean>(true);
  selectedCountryId = signal<string>('');

  // City list (dependent on province)
  cities = signal<City[]>([]);
  isLoadingCities = signal<boolean>(true);
  selectedProvinceId = signal<string>('');

  // Category list
  categories = signal<JobCategory[]>([]);
  isLoadingCategories = signal<boolean>(true);

  // Skill list
  skills = signal<Skill[]>([]);
  isLoadingSkills = signal<boolean>(true);

  // Form state
  isCreating = signal<boolean>(false);
  isEditing = signal<boolean>(false);
  editingId = signal<string | null>(null);
  selectedEntity = signal<EntityType>('countries');
  formError = signal<string | null>(null);

  // Form values
  countryForm: CountryForm = { name: '', code: '' };
  provinceForm: ProvinceForm = { countryId: '', name: '' };
  cityForm: CityForm = { countryId: '', provinceId: '', name: '' };
  namedForm: NamedForm = { name: '', description: null };

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.loadCountries();
    this.loadCategories();
    this.loadSkills();
  }

    switchTab(tab: EntityType): void {
    if (this.isCreating() || this.isEditing()) {
      this.cancelForm();
    }
    this.activeTab.set(tab);
  }

  // ── Country CRUD ──────────────────────────────────────────

  loadCountries(): void {
    this.isLoadingCountries.set(true);
    this.refService.getAllCountries().subscribe({
      next: (data) => this.countries.set(data),
      error: () => {
        this.countries.set([]);
        this.isLoadingCountries.set(false);
      },
      complete: () => this.isLoadingCountries.set(false),
    });
  }

  startCreateCountry(): void {
    this.selectedEntity.set('countries');
    this.isCreating.set(true);
    this.isEditing.set(false);
    this.editingId.set(null);
    this.formError.set(null);
    this.countryForm = { name: '', code: '' };
  }

  startEditCountry(country: Country): void {
    this.selectedEntity.set('countries');
    this.isCreating.set(false);
    this.isEditing.set(true);
    this.editingId.set(country.id);
    this.formError.set(null);
    this.countryForm = { name: country.name, code: country.code };
  }

  submitCountry(): void {
    if (!this.countryForm.name.trim() || !this.countryForm.code.trim()) {
      this.formError.set('نام و کد کشور الزامی هستند.');
      return;
    }
    if (this.countryForm.code.trim().length > 3) {
      this.formError.set('کد کشور حداکثر ۳ کاراکتر است.');
      return;
    }

    const input = {
      name: this.countryForm.name.trim(),
      code: this.countryForm.code.trim(),
    };

    if (this.isEditing() && this.editingId()) {
      this.refService.updateCountry(this.editingId()!, input).subscribe({
        next: () => {
          this.loadCountries();
          this.cancelForm();
        },
        error: (err) => {
          this.formError.set(err.status === 409 ? 'کد کشور تکراری است.' : 'خطا در بروزرسانی کشور.');
        },
      });
    } else {
      this.refService.createCountry(input).subscribe({
        next: () => {
          this.loadCountries();
          this.cancelForm();
        },
        error: (err) => {
          this.formError.set(err.status === 409 ? 'کد کشور تکراری است.' : 'خطا در ایجاد کشور.');
        },
      });
    }
  }

  deactivateCountry(country: Country): void {
    if (!confirm(`آیا مطمئن هستید که می‌خواهید کشور "${country.name}" را غیرفعال کنید؟`)) return;
    this.refService.deactivateCountry(country.id).subscribe({
      next: () => this.loadCountries(),
            error: (err) => {
        alert(err.status === 409
          ? 'این کشور دارای استان فعال است و نمی‌تواند غیرفعال شود.'
          : 'خطا در غیرفعال‌سازی کشور.');
      },
    });
  }

  // ── Province CRUD ─────────────────────────────────────────

  loadProvinces(): void {
    if (!this.selectedCountryId()) {
      this.provinces.set([]);
      this.isLoadingProvinces.set(false);
      return;
    }
    this.isLoadingProvinces.set(true);
    this.refService.getAllProvinces(this.selectedCountryId()).subscribe({
      next: (data) => {
        this.provinces.set(data);
        if (data.length > 0 && !this.selectedProvinceId()) {
          this.selectedProvinceId.set(data[0].id);
          this.loadCities();
        }
      },
      error: () => {
        this.provinces.set([]);
        this.isLoadingProvinces.set(false);
      },
      complete: () => this.isLoadingProvinces.set(false),
    });
  }

  onCountryFilterChange(event: Event): void {
    const countryId = (event.target as HTMLSelectElement)?.value ?? '';
    this.selectedCountryId.set(countryId);
    this.selectedProvinceId.set('');
    this.cities.set([]);
    if (countryId) {
      this.loadProvinces();
    }
  }

  startCreateProvince(): void {
    if (!this.selectedCountryId()) {
      alert('لطفاً ابتدا یک کشور انتخاب کنید.');
      return;
    }
    this.selectedEntity.set('provinces');
    this.isCreating.set(true);
    this.isEditing.set(false);
    this.editingId.set(null);
    this.formError.set(null);
    this.provinceForm = { countryId: this.selectedCountryId(), name: '' };
  }

  startEditProvince(province: Province): void {
    this.selectedEntity.set('provinces');
    this.isCreating.set(false);
    this.isEditing.set(true);
    this.editingId.set(province.id);
    this.formError.set(null);
    this.provinceForm = { countryId: province.countryId, name: province.name };
  }

  submitProvince(): void {
    if (!this.provinceForm.name.trim()) {
      this.formError.set('نام استان الزامی است.');
      return;
    }

    const input = {
      countryId: this.provinceForm.countryId,
      name: this.provinceForm.name.trim(),
    };

    if (this.isEditing() && this.editingId()) {
      this.refService.updateProvince(this.editingId()!, input).subscribe({
        next: () => {
          this.loadProvinces();
          this.cancelForm();
        },
        error: (err) => {
          this.formError.set(err.status === 409 ? 'نام استان تکراری است.' : 'خطا در بروزرسانی استان.');
        },
      });
    } else {
      this.refService.createProvince(input).subscribe({
        next: () => {
          this.loadProvinces();
          this.cancelForm();
        },
        error: (err) => {
          this.formError.set(err.status === 409 ? 'نام استان تکراری است.' : 'خطا در ایجاد استان.');
        },
      });
    }
  }

  deactivateProvince(province: Province): void {
    if (!confirm(`آیا مطمئن هستید که می‌خواهید استان "${province.name}" را غیرفعال کنید؟`)) return;
    this.refService.deactivateProvince(province.id).subscribe({
      next: () => this.loadProvinces(),
            error: (err) => {
        alert(err.status === 409
          ? 'این استان دارای شهر فعال است و نمی‌تواند غیرفعال شود.'
          : 'خطا در غیرفعال‌سازی استان.');
      },
    });
  }

  // ── City CRUD ─────────────────────────────────────────────

  loadCities(): void {
    if (!this.selectedProvinceId()) {
      this.cities.set([]);
      this.isLoadingCities.set(false);
      return;
    }
    this.isLoadingCities.set(true);
    this.refService.getCitiesByProvince(this.selectedProvinceId()).subscribe({
      next: (data) => this.cities.set(data),
      error: () => {
        this.cities.set([]);
        this.isLoadingCities.set(false);
      },
      complete: () => this.isLoadingCities.set(false),
    });
  }

  onProvinceFilterChange(event: Event): void {
    const provinceId = (event.target as HTMLSelectElement)?.value ?? '';
    this.selectedProvinceId.set(provinceId);
    if (provinceId) {
      this.loadCities();
    } else {
      this.cities.set([]);
    }
  }

  loadCitiesForCountry(countryIdOrEvent: string | Event): void {
    const countryId = typeof countryIdOrEvent === 'string'
      ? countryIdOrEvent
      : (countryIdOrEvent.target as HTMLSelectElement)?.value ?? '';
    this.selectedCountryId.set(countryId);
    this.selectedProvinceId.set('');
    this.provinces.set([]);
    this.cities.set([]);
    if (countryId) {
      this.isLoadingProvinces.set(true);
      this.refService.getAllProvinces(countryId).subscribe({
        next: (data) => {
          this.provinces.set(data);
          if (data.length > 0) {
            this.selectedProvinceId.set(data[0].id);
            this.loadCities();
          }
        },
        error: () => {
          this.provinces.set([]);
          this.isLoadingProvinces.set(false);
        },
        complete: () => this.isLoadingProvinces.set(false),
      });
    }
  }

  startCreateCity(): void {
    if (!this.selectedProvinceId()) {
      alert('لطفاً ابتدا یک استان انتخاب کنید.');
      return;
    }
    const province = this.provinces().find((p) => p.id === this.selectedProvinceId());
    this.selectedEntity.set('cities');
    this.isCreating.set(true);
    this.isEditing.set(false);
    this.editingId.set(null);
    this.formError.set(null);
    this.cityForm = {
      countryId: province ? province.countryId : this.selectedCountryId(),
      provinceId: this.selectedProvinceId(),
      name: '',
    };
  }

  startEditCity(city: City): void {
    this.selectedEntity.set('cities');
    this.isCreating.set(false);
    this.isEditing.set(true);
    this.editingId.set(city.id);
    this.formError.set(null);
    this.cityForm = {
      countryId: city.countryId,
      provinceId: city.provinceId,
      name: city.name,
    };
  }

  submitCity(): void {
    if (!this.cityForm.name.trim()) {
      this.formError.set('نام شهر الزامی است.');
      return;
    }

    const input = {
      provinceId: this.cityForm.provinceId,
      name: this.cityForm.name.trim(),
    };

    if (this.isEditing() && this.editingId()) {
      this.refService.updateCity(this.editingId()!, input).subscribe({
        next: () => {
          this.loadCities();
          this.cancelForm();
        },
        error: (err) => {
          this.formError.set(err.status === 409 ? 'نام شهر تکراری است.' : 'خطا در بروزرسانی شهر.');
        },
      });
    } else {
      this.refService.createCity(input).subscribe({
        next: () => {
          this.loadCities();
          this.cancelForm();
        },
        error: (err) => {
          this.formError.set(err.status === 409 ? 'نام شهر تکراری است.' : 'خطا در ایجاد شهر.');
        },
      });
    }
  }

  deactivateCity(city: City): void {
    if (!confirm(`آیا مطمئن هستید که می‌خواهید شهر "${city.name}" را غیرفعال کنید؟`)) return;
    this.refService.deactivateCity(city.id).subscribe({
      next: () => this.loadCities(),
      error: (err) => {
        alert(err.status === 409
          ? 'قابل غیرفعال‌سازی نیست.'
          : 'خطا در غیرفعال‌سازی شهر.');
      },
    });
  }

  // ── Job Category CRUD ─────────────────────────────────────

  loadCategories(): void {
    this.isLoadingCategories.set(true);
    this.refService.getAllCategories().subscribe({
      next: (data) => this.categories.set(data),
      error: () => {
        this.categories.set([]);
        this.isLoadingCategories.set(false);
      },
      complete: () => this.isLoadingCategories.set(false),
    });
  }

  startCreateCategory(): void {
    this.selectedEntity.set('categories');
    this.isCreating.set(true);
    this.isEditing.set(false);
    this.editingId.set(null);
    this.formError.set(null);
    this.namedForm = { name: '', description: null };
  }

  startEditCategory(category: JobCategory): void {
    this.selectedEntity.set('categories');
    this.isCreating.set(false);
    this.isEditing.set(true);
    this.editingId.set(category.id);
    this.formError.set(null);
    this.namedForm = { name: category.name, description: category.description };
  }

  submitCategory(): void {
    if (!this.namedForm.name.trim()) {
      this.formError.set('نام دسته‌بندی الزامی است.');
      return;
    }

    const input = {
      name: this.namedForm.name.trim(),
      description: this.namedForm.description?.trim() || null,
    };

    if (this.isEditing() && this.editingId()) {
      this.refService.updateCategory(this.editingId()!, input).subscribe({
        next: () => {
          this.loadCategories();
          this.cancelForm();
        },
        error: (err) => {
          this.formError.set(err.status === 409 ? 'نام دسته‌بندی تکراری است.' : 'خطا در بروزرسانی دسته‌بندی.');
        },
      });
    } else {
      this.refService.createCategory(input).subscribe({
        next: () => {
          this.loadCategories();
          this.cancelForm();
        },
        error: (err) => {
          this.formError.set(err.status === 409 ? 'نام دسته‌بندی تکراری است.' : 'خطا در ایجاد دسته‌بندی.');
        },
      });
    }
  }

  deactivateCategory(category: JobCategory): void {
    if (!confirm(`آیا مطمئن هستید که می‌خواهید دسته‌بندی "${category.name}" را غیرفعال کنید؟`)) return;
    this.refService.deactivateCategory(category.id).subscribe({
      next: () => this.loadCategories(),
          });
  }

  // ── Skill CRUD ────────────────────────────────────────────

  loadSkills(): void {
    this.isLoadingSkills.set(true);
    this.refService.getAllSkills().subscribe({
      next: (data) => this.skills.set(data),
      error: () => {
        this.skills.set([]);
        this.isLoadingSkills.set(false);
      },
      complete: () => this.isLoadingSkills.set(false),
    });
  }

  startCreateSkill(): void {
    this.selectedEntity.set('skills');
    this.isCreating.set(true);
    this.isEditing.set(false);
    this.editingId.set(null);
    this.formError.set(null);
    this.namedForm = { name: '', description: null };
  }

  startEditSkill(skill: Skill): void {
    this.selectedEntity.set('skills');
    this.isCreating.set(false);
    this.isEditing.set(true);
    this.editingId.set(skill.id);
    this.formError.set(null);
    this.namedForm = { name: skill.name, description: skill.description };
  }

  submitSkill(): void {
    if (!this.namedForm.name.trim()) {
      this.formError.set('نام مهارت الزامی است.');
      return;
    }

    const input = {
      name: this.namedForm.name.trim(),
      description: this.namedForm.description?.trim() || null,
    };

    if (this.isEditing() && this.editingId()) {
      this.refService.updateSkill(this.editingId()!, input).subscribe({
        next: () => {
          this.loadSkills();
          this.cancelForm();
        },
        error: (err) => {
          this.formError.set(err.status === 409 ? 'نام مهارت تکراری است.' : 'خطا در بروزرسانی مهارت.');
        },
      });
    } else {
      this.refService.createSkill(input).subscribe({
        next: () => {
          this.loadSkills();
          this.cancelForm();
        },
        error: (err) => {
          this.formError.set(err.status === 409 ? 'نام مهارت تکراری است.' : 'خطا در ایجاد مهارت.');
        },
      });
    }
  }

  deactivateSkill(skill: Skill): void {
    if (!confirm(`آیا مطمئن هستید که می‌خواهید مهارت "${skill.name}" را غیرفعال کنید؟`)) return;
    this.refService.deactivateSkill(skill.id).subscribe({
      next: () => this.loadSkills(),
      error: () => alert('خطا در غیرفعال‌سازی مهارت.'),
    });
  }

  // ── Form helpers ──────────────────────────────────────────

  cancelForm(): void {
    this.isCreating.set(false);
    this.isEditing.set(false);
    this.editingId.set(null);
    this.formError.set(null);
    this.countryForm = { name: '', code: '' };
    this.provinceForm = { countryId: '', name: '' };
    this.cityForm = { countryId: '', provinceId: '', name: '' };
    this.namedForm = { name: '', description: null };
  }

  get isFormOpen(): boolean {
    return this.isCreating() || this.isEditing();
  }

  get entityLabel(): string {
    const labels: Record<EntityType, string> = {
      countries: 'کشور',
      provinces: 'استان',
      cities: 'شهر',
      categories: 'دسته‌بندی شغلی',
      skills: 'مهارت',
    };
    return labels[this.selectedEntity()];
  }

  reloadCurrentList(): void {
    switch (this.activeTab()) {
      case 'countries': this.loadCountries(); break;
      case 'provinces': this.loadProvinces(); break;
      case 'cities': this.loadCities(); break;
      case 'categories': this.loadCategories(); break;
      case 'skills': this.loadSkills(); break;
    }
  }

  get activeCountries(): Country[] {
    return this.countries().filter((c) => c.isActive);
  }

  get inactiveCountriesCount(): number {
    return this.countries().filter((c) => !c.isActive).length;
  }

  formatDate(dateString: string | null): string {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString('fa-IR', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    });
  }
}
