import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { JobCategorySelectComponent } from '../components/job-category-select.component';
import { LocationSelectComponent } from '../components/location-select.component';
import { SkillSelectComponent } from '../components/skill-select.component';
import { JobCategoryService } from './job-category.service';
import { LocationService } from './location.service';
import { SkillService } from './skill.service';

describe('Reference data services and selectors', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [JobCategorySelectComponent, SkillSelectComponent, LocationSelectComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('requests job categories from the versioned public endpoint', () => {
    TestBed.inject(JobCategoryService).getAll().subscribe(items => expect(items).toEqual([]));
    const request = httpMock.expectOne('http://localhost:5158/api/v1/job-categories');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('requests a skill by id from the versioned public endpoint', () => {
    TestBed.inject(SkillService).getById('skill-1').subscribe(item => expect(item.id).toBe('skill-1'));
    const request = httpMock.expectOne('http://localhost:5158/api/v1/skills/skill-1');
    expect(request.request.method).toBe('GET');
    request.flush({ id: 'skill-1', name: 'C#', description: null, isActive: true, createdAt: '', updatedAt: null });
  });

  it('sends location parent ids as query parameters', () => {
    TestBed.inject(LocationService).getProvinces('country-1').subscribe(items => expect(items).toEqual([]));
    const request = httpMock.expectOne('http://localhost:5158/api/v1/locations/provinces?countryId=country-1');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('shows category loading state and renders the API options', () => {
    const fixture = TestBed.createComponent(JobCategorySelectComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="status"]')).not.toBeNull();
    httpMock.expectOne('http://localhost:5158/api/v1/job-categories').flush([
      { id: 'cat-1', name: 'Engineering', description: null, isActive: true, createdAt: '', updatedAt: null }
    ]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('select option:last-child')?.textContent).toContain('Engineering');
  });

  it('shows skill API errors and leaves loading state', () => {
    const fixture = TestBed.createComponent(SkillSelectComponent);
    fixture.detectChanges();
    httpMock.expectOne('http://localhost:5158/api/v1/skills').flush('unavailable', { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain('Unable to load skills.');
    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('select').disabled).toBeTrue();
  });

  it('loads provinces and cities in order from the selected location parents', () => {
    const fixture: ComponentFixture<LocationSelectComponent> = TestBed.createComponent(LocationSelectComponent);
    fixture.detectChanges();
    httpMock.expectOne('http://localhost:5158/api/v1/locations/countries').flush([
      { id: 'country-1', name: 'Iran', code: 'IR', isActive: true, createdAt: '', updatedAt: null }
    ]);
    fixture.detectChanges();

    const countrySelect = fixture.nativeElement.querySelectorAll('select')[0] as HTMLSelectElement;
    countrySelect.value = 'country-1';
    countrySelect.dispatchEvent(new Event('change'));
    const provincesRequest = httpMock.expectOne('http://localhost:5158/api/v1/locations/provinces?countryId=country-1');
    provincesRequest.flush([
      { id: 'province-1', countryId: 'country-1', countryName: 'Iran', name: 'Tehran', isActive: true, createdAt: '', updatedAt: null }
    ]);
    fixture.detectChanges();

    const provinceSelect = fixture.nativeElement.querySelectorAll('select')[1] as HTMLSelectElement;
    provinceSelect.value = 'province-1';
    provinceSelect.dispatchEvent(new Event('change'));
    const citiesRequest = httpMock.expectOne('http://localhost:5158/api/v1/locations/cities?provinceId=province-1');
    citiesRequest.flush([
      { id: 'city-1', provinceId: 'province-1', provinceName: 'Tehran', countryId: 'country-1', countryName: 'Iran', name: 'Tehran', isActive: true, createdAt: '', updatedAt: null }
    ]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('select')[2].options.length).toBe(2);
    expect(fixture.nativeElement.querySelectorAll('select')[2].options[1].textContent).toContain('Tehran');
  });

  it('keeps the selected hierarchy when the form echoes the selected city id', () => {
    const fixture = TestBed.createComponent(LocationSelectComponent);
    fixture.componentInstance.cityIdChange.subscribe(cityId => fixture.componentRef.setInput('cityId', cityId));
    fixture.detectChanges();
    httpMock.expectOne('http://localhost:5158/api/v1/locations/countries').flush([
      { id: 'country-1', name: 'Iran', code: 'IR', isActive: true, createdAt: '', updatedAt: null }
    ]);
    fixture.detectChanges();

    let selects = fixture.nativeElement.querySelectorAll('select') as NodeListOf<HTMLSelectElement>;
    selects[0].value = 'country-1';
    selects[0].dispatchEvent(new Event('change'));
    httpMock.expectOne('http://localhost:5158/api/v1/locations/provinces?countryId=country-1').flush([
      { id: 'province-1', countryId: 'country-1', countryName: 'Iran', name: 'Tehran Province', isActive: true, createdAt: '', updatedAt: null }
    ]);
    fixture.detectChanges();

    selects = fixture.nativeElement.querySelectorAll('select');
    selects[1].value = 'province-1';
    selects[1].dispatchEvent(new Event('change'));
    httpMock.expectOne('http://localhost:5158/api/v1/locations/cities?provinceId=province-1').flush([
      { id: 'city-1', provinceId: 'province-1', provinceName: 'Tehran Province', countryId: 'country-1', countryName: 'Iran', name: 'Tehran', isActive: true, createdAt: '', updatedAt: null }
    ]);
    fixture.detectChanges();

    selects = fixture.nativeElement.querySelectorAll('select');
    selects[2].value = 'city-1';
    selects[2].dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.componentInstance.selectedCountryId).toBe('country-1');
    expect(fixture.componentInstance.selectedProvinceId).toBe('province-1');
    expect(fixture.componentInstance.cityId).toBe('city-1');
    httpMock.expectNone('http://localhost:5158/api/v1/locations/city-1');
  });

  it('restores a selected city through its country and province hierarchy', () => {
    const fixture = TestBed.createComponent(LocationSelectComponent);
    fixture.detectChanges();
    httpMock.expectOne('http://localhost:5158/api/v1/locations/countries').flush([
      { id: 'country-1', name: 'Iran', code: 'IR', isActive: true, createdAt: '', updatedAt: null }
    ]);
    fixture.componentRef.setInput('cityId', 'city-1');
    fixture.detectChanges();
    httpMock.expectOne('http://localhost:5158/api/v1/locations/city-1').flush({
      id: 'city-1', name: 'Tehran', type: 'City', parentId: 'province-1', parentName: 'Tehran Province',
      countryId: 'country-1', countryName: 'Iran', isActive: true
    });
    httpMock.expectOne('http://localhost:5158/api/v1/locations/provinces?countryId=country-1').flush([
      { id: 'province-1', countryId: 'country-1', countryName: 'Iran', name: 'Tehran Province', isActive: true, createdAt: '', updatedAt: null }
    ]);
    httpMock.expectOne('http://localhost:5158/api/v1/locations/cities?provinceId=province-1').flush([
      { id: 'city-1', provinceId: 'province-1', provinceName: 'Tehran Province', countryId: 'country-1', countryName: 'Iran', name: 'Tehran', isActive: true, createdAt: '', updatedAt: null }
    ]);
    fixture.detectChanges();

    const selects = fixture.nativeElement.querySelectorAll('select') as NodeListOf<HTMLSelectElement>;
    expect(fixture.componentInstance.selectedCountryId).toBe('country-1');
    expect(fixture.componentInstance.selectedProvinceId).toBe('province-1');
    expect(fixture.componentInstance.cityId).toBe('city-1');
    expect(selects[0].value).toBe('country-1');
    expect(selects[1].value).toBe('province-1');
    expect(selects[2].value).toBe('city-1');
  });
});
