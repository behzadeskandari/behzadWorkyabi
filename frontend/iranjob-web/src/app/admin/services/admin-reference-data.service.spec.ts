import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AdminReferenceDataService } from './admin-reference-data.service';

const API = 'http://localhost:5158/api/v1';

describe('AdminReferenceDataService', () => {
  let service: AdminReferenceDataService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AdminReferenceDataService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  // ── Countries ─────────────────────────────────────────────

  it('requests all countries from the locations endpoint', () => {
    service.getAllCountries().subscribe((items) => expect(items).toEqual([]));
    const req = httpMock.expectOne(`${API}/locations/countries`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('creates a country via POST', () => {
    const input = { name: 'Test Country', code: 'TC' };
    service.createCountry(input).subscribe((result) => {
      expect(result.id).toBe('country-1');
    });
    const req = httpMock.expectOne(`${API}/locations/countries`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(input);
    req.flush({ id: 'country-1', name: 'Test Country', code: 'TC', isActive: true, createdAt: '', updatedAt: null });
  });

  it('updates a country via PUT', () => {
    const input = { name: 'Updated Country', code: 'UC' };
    service.updateCountry('country-1', input).subscribe((result) => {
      expect(result.name).toBe('Updated Country');
    });
    const req = httpMock.expectOne(`${API}/locations/countries/country-1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(input);
    req.flush({ id: 'country-1', name: 'Updated Country', code: 'UC', isActive: true, createdAt: '', updatedAt: null });
  });

  it('deactivates a country via DELETE', () => {
    service.deactivateCountry('country-1').subscribe({ complete: () => expect(true).toBeTrue() });
    const req = httpMock.expectOne(`${API}/locations/countries/country-1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  // ── Provinces ─────────────────────────────────────────────

  it('requests provinces filtered by countryId query param', () => {
    service.getAllProvinces('country-1').subscribe((items) => expect(items).toEqual([]));
    const req = httpMock.expectOne(`${API}/locations/provinces?countryId=country-1`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('creates a province with countryId', () => {
    service.createProvince({ countryId: 'country-1', name: 'Tehran' }).subscribe((result) => {
      expect(result.name).toBe('Tehran');
    });
    const req = httpMock.expectOne(`${API}/locations/provinces`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ countryId: 'country-1', name: 'Tehran' });
    req.flush({ id: 'prov-1', countryId: 'country-1', countryName: 'Test Country', name: 'Tehran', isActive: true, createdAt: '', updatedAt: null });
  });

  // ── Cities ────────────────────────────────────────────────

  it('requests cities filtered by provinceId query param', () => {
    service.getCitiesByProvince('prov-1').subscribe((items) => expect(items).toEqual([]));
    const req = httpMock.expectOne(`${API}/locations/cities?provinceId=prov-1`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('deactivates a city via DELETE', () => {
    service.deactivateCity('city-1').subscribe({ complete: () => expect(true).toBeTrue() });
    const req = httpMock.expectOne(`${API}/locations/cities/city-1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  // ── Job Categories ────────────────────────────────────────

  it('requests all categories from the job-categories endpoint', () => {
    service.getAllCategories().subscribe((items) => expect(items).toEqual([]));
    const req = httpMock.expectOne(`${API}/job-categories`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('creates a category via POST', () => {
    const input = { name: 'Engineering', description: 'Tech roles' };
    service.createCategory(input).subscribe((result) => {
      expect(result.name).toBe('Engineering');
    });
    const req = httpMock.expectOne(`${API}/job-categories`);
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'cat-1', name: 'Engineering', description: 'Tech roles', isActive: true, createdAt: '', updatedAt: null });
  });

  // ── Skills ────────────────────────────────────────────────

  it('requests all skills from the skills endpoint', () => {
    service.getAllSkills().subscribe((items) => expect(items).toEqual([]));
    const req = httpMock.expectOne(`${API}/skills`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('updates a skill via PUT', () => {
    const input = { name: 'Angular', description: null };
    service.updateSkill('skill-1', input).subscribe((result) => {
      expect(result.name).toBe('Angular');
    });
    const req = httpMock.expectOne(`${API}/skills/skill-1`);
    expect(req.request.method).toBe('PUT');
    req.flush({ id: 'skill-1', name: 'Angular', description: null, isActive: true, createdAt: '', updatedAt: null });
  });
});
