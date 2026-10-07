import { CommonModule } from '@angular/common';
import { Component, DestroyRef, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { City, Country, Province } from '../models/reference-data.models';
import { LocationService } from '../services/location.service';

@Component({
  selector: 'app-location-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './location-select.component.html'
})
export class LocationSelectComponent implements OnChanges, OnInit {
  @Input() cityId: string | null = null;
  @Input() labelPrefix = 'Work location';
  @Output() readonly cityIdChange = new EventEmitter<string | null>();

  countries: Country[] = [];
  provinces: Province[] = [];
  cities: City[] = [];
  selectedCountryId = '';
  selectedProvinceId = '';
  isLoadingCountries = false;
  isLoadingProvinces = false;
  isLoadingCities = false;
  errorMessage = '';

  private readonly destroyRef = inject(DestroyRef);
  private readonly locationService = inject(LocationService);

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['cityId'] || !this.countries.length || !this.cityId || this.cities.some(city => city.id === this.cityId)) return;
    this.selectedCountryId = '';
    this.selectedProvinceId = '';
    this.provinces = [];
    this.cities = [];
    if (this.cityId) this.loadSelectedCity(this.cityId);
  }

  ngOnInit(): void {
    this.loadCountries();
  }

  loadCountries(): void {
    this.isLoadingCountries = true;
    this.errorMessage = '';
    this.locationService.getCountries().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: countries => {
        this.countries = countries;
        if (this.cityId) this.loadSelectedCity(this.cityId);
      },
      error: () => {
        this.errorMessage = 'Unable to load countries.';
        this.isLoadingCountries = false;
      },
      complete: () => this.isLoadingCountries = false
    });
  }

  onCountryChange(event: Event): void {
    this.selectedCountryId = (event.target as HTMLSelectElement).value;
    this.selectedProvinceId = '';
    this.provinces = [];
    this.cities = [];
    this.cityId = null;
    this.errorMessage = '';
    this.cityIdChange.emit(null);
    if (!this.selectedCountryId) return;

    this.loadProvinces();
  }

  onProvinceChange(event: Event): void {
    this.selectedProvinceId = (event.target as HTMLSelectElement).value;
    this.cities = [];
    this.cityId = null;
    this.errorMessage = '';
    this.cityIdChange.emit(null);
    if (!this.selectedProvinceId) return;

    this.loadCities();
  }

  retry(): void {
    if (this.errorMessage.includes('provinces') && this.selectedCountryId) {
      this.loadProvinces();
    } else if (this.errorMessage.includes('cities') && this.selectedProvinceId) {
      this.loadCities();
    } else {
      this.loadCountries();
    }
  }

  onCityChange(event: Event): void {
    const selectedId = (event.target as HTMLSelectElement).value;
    this.cityId = selectedId || null;
    this.cityIdChange.emit(selectedId || null);
  }

  private loadSelectedCity(id: string): void {
    this.locationService.getById(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: location => {
        if (location.type !== 'City' || !location.countryId || !location.parentId) {
          this.errorMessage = 'Unable to load the selected city.';
          return;
        }
        this.selectedCountryId = location.countryId;
        this.selectedProvinceId = location.parentId;
        this.loadProvinces();
      },
      error: () => this.errorMessage = 'Unable to load the selected city.'
    });
  }

  private loadProvinces(): void {
    this.isLoadingProvinces = true;
    this.errorMessage = '';
    this.locationService.getProvinces(this.selectedCountryId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: provinces => {
        this.provinces = provinces;
        if (this.cityId) this.loadCities();
      },
      error: () => {
        this.errorMessage = 'Unable to load provinces.';
        this.isLoadingProvinces = false;
      },
      complete: () => this.isLoadingProvinces = false
    });
  }

  private loadCities(): void {
    this.isLoadingCities = true;
    this.errorMessage = '';
    this.locationService.getCities(this.selectedProvinceId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: cities => this.cities = cities,
      error: () => {
        this.errorMessage = 'Unable to load cities.';
        this.isLoadingCities = false;
      },
      complete: () => this.isLoadingCities = false
    });
  }
}
