import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { EmployerJobListComponent } from '../pages/jobs/job-list.component';
import { EmployerJobFormComponent } from '../pages/jobs/job-form.component';
import { JobPosting } from '../models/job-posting.models';
import { JobPostingService } from './job-posting.service';

describe('JobPostingService and employer job pages', () => {
  let httpMock: HttpTestingController;
  let routeId: string | null;

  beforeEach(() => {
    routeId = null;
    TestBed.configureTestingModule({
      imports: [EmployerJobListComponent, EmployerJobFormComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => routeId } } } }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('uses the employer-owned jobs endpoints for CRUD and lifecycle actions', () => {
    const service = TestBed.inject(JobPostingService);
    const request = validRequest();
    const job = jobResponse();
    service.getOwn().subscribe();
    httpMock.expectOne('http://localhost:5158/api/v1/employers/jobs').flush([job]);

    service.getById(job.id).subscribe();
    httpMock.expectOne(`http://localhost:5158/api/v1/employers/jobs/${job.id}`).flush(job);

    service.create(request).subscribe();
    const createRequest = httpMock.expectOne('http://localhost:5158/api/v1/employers/jobs');
    expect(createRequest.request.method).toBe('POST');
    createRequest.flush(job);

    service.update(job.id, request).subscribe();
    const updateRequest = httpMock.expectOne(`http://localhost:5158/api/v1/employers/jobs/${job.id}`);
    expect(updateRequest.request.method).toBe('PUT');
    updateRequest.flush(job);

    service.publish(job.id).subscribe();
    const publishRequest = httpMock.expectOne(`http://localhost:5158/api/v1/employers/jobs/${job.id}/publish`);
    expect(publishRequest.request.method).toBe('POST');
    publishRequest.flush(job);

    service.unpublish(job.id).subscribe();
    const unpublishRequest = httpMock.expectOne(`http://localhost:5158/api/v1/employers/jobs/${job.id}/unpublish`);
    expect(unpublishRequest.request.method).toBe('POST');
    unpublishRequest.flush(job);

    service.close(job.id).subscribe();
    const closeRequest = httpMock.expectOne(`http://localhost:5158/api/v1/employers/jobs/${job.id}/close`);
    expect(closeRequest.request.method).toBe('POST');
    closeRequest.flush(job);
  });

  it('loads the employer job list and exposes draft publishing', () => {
    const fixture: ComponentFixture<EmployerJobListComponent> = TestBed.createComponent(EmployerJobListComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="status"]')).not.toBeNull();

    httpMock.expectOne('http://localhost:5158/api/v1/employers/jobs').flush([jobResponse()]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Senior .NET Engineer');
    const buttons = fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>;
    const publishButton = Array.from(buttons).find(button => button.textContent?.includes('انتشار'));
    expect(publishButton).toBeDefined();
    publishButton?.click();

    const publishRequest = httpMock.expectOne('http://localhost:5158/api/v1/employers/jobs/job-1/publish');
    expect(publishRequest.request.method).toBe('POST');
    publishRequest.flush({ ...jobResponse(), status: 'Published' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('لغو انتشار');

    const closeButtons = fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>;
    const closeButton = Array.from(closeButtons).find(button => button.textContent?.includes('بستن آگهی'));
    expect(closeButton).toBeDefined();
    closeButton?.click();
    const closeRequest = httpMock.expectOne('http://localhost:5158/api/v1/employers/jobs/job-1/close');
    closeRequest.flush({ ...jobResponse(), status: 'Closed' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('بسته‌شده');
    expect(fixture.nativeElement.textContent).not.toContain('ویرایش');
  });

  it('creates a draft from the form without client ownership or lifecycle fields', () => {
    const router = TestBed.inject(Router);
    spyOn(router, 'navigate');
    const fixture = TestBed.createComponent(EmployerJobFormComponent);
    fixture.detectChanges();
    httpMock.expectOne('http://localhost:5158/api/v1/job-categories').flush([]);
    httpMock.expectOne('http://localhost:5158/api/v1/skills').flush([]);
    httpMock.expectOne('http://localhost:5158/api/v1/locations/countries').flush([]);

    fixture.componentInstance.jobForm.patchValue({
      title: 'Frontend Engineer',
      description: 'Build excellent user experiences.',
      categoryId: 'category-1',
      skillIds: ['skill-1'],
      employmentType: 'FullTime',
      workArrangement: 'Remote',
      cityId: 'city-1',
      salaryMinimum: null,
      salaryMaximum: null
    });
    fixture.detectChanges();

    fixture.componentInstance.saveDraft();
    const request = httpMock.expectOne('http://localhost:5158/api/v1/employers/jobs');
    expect(request.request.method).toBe('POST');
    expect(request.request.body.categoryId).toBe('category-1');
    expect(request.request.body.skillIds).toEqual(['skill-1']);
    expect(request.request.body.cityId).toBe('city-1');
    expect(request.request.body.userId).toBeUndefined();
    expect(request.request.body.employerProfileId).toBeUndefined();
    expect(request.request.body.status).toBeUndefined();
    request.flush(jobResponse());
    expect(router.navigate).toHaveBeenCalledWith(['/employer/jobs']);
  });

  it('loads an existing job into the edit form', () => {
    routeId = 'job-1';
    const fixture = TestBed.createComponent(EmployerJobFormComponent);
    fixture.detectChanges();

    httpMock.expectOne('http://localhost:5158/api/v1/employers/jobs/job-1').flush(jobResponse());
    fixture.detectChanges();
    httpMock.expectOne('http://localhost:5158/api/v1/job-categories').flush([]);
    httpMock.expectOne('http://localhost:5158/api/v1/skills').flush([]);
    httpMock.expectOne('http://localhost:5158/api/v1/locations/countries').flush([]);
    fixture.detectChanges();

    expect(fixture.componentInstance.jobForm.controls.title.value).toBe('Senior .NET Engineer');
    expect(fixture.componentInstance.jobForm.controls.categoryId.value).toBe('category-1');
    expect(fixture.componentInstance.currentStatus).toBe('Draft');
  });

  it('blocks invalid form submission and maps API validation errors', () => {
    const fixture = TestBed.createComponent(EmployerJobFormComponent);
    fixture.detectChanges();
    httpMock.expectOne('http://localhost:5158/api/v1/job-categories').flush([]);
    httpMock.expectOne('http://localhost:5158/api/v1/skills').flush([]);
    httpMock.expectOne('http://localhost:5158/api/v1/locations/countries').flush([]);

    fixture.componentInstance.saveDraft();
    expect(fixture.componentInstance.jobForm.invalid).toBeTrue();
    httpMock.expectNone('http://localhost:5158/api/v1/employers/jobs');

    fixture.componentInstance.jobForm.patchValue({
      title: 'Frontend Engineer',
      description: 'Build excellent user experiences.',
      categoryId: 'category-1',
      skillIds: ['skill-1'],
      employmentType: 'FullTime',
      workArrangement: 'Remote',
      cityId: 'city-1'
    });
    fixture.componentInstance.saveDraft();
    httpMock.expectOne('http://localhost:5158/api/v1/employers/jobs').flush({
      errors: { Title: ['Title is invalid.'] }
    }, { status: 400, statusText: 'Bad Request' });

    expect(fixture.componentInstance.errorMessage).toContain('معتبر نیست');
    expect(fixture.componentInstance.messagesFor('Title')).toEqual(['Title is invalid.']);
  });
});

function validRequest() {
  return {
    title: 'Senior .NET Engineer',
    description: 'Build reliable services.',
    categoryId: 'category-1',
    skillIds: ['skill-1'],
    employmentType: 'FullTime' as const,
    workArrangement: 'Remote' as const,
    cityId: null,
    salaryMinimum: null,
    salaryMaximum: null,
    salaryCurrency: null,
    salaryPeriod: null,
    closingAt: null
  };
}

function jobResponse(): JobPosting {
  return {
    id: 'job-1',
    title: 'Senior .NET Engineer',
    description: 'Build reliable services.',
    categoryId: 'category-1',
    categoryName: 'Engineering',
    skills: [{ id: 'skill-1', name: 'C#' }],
    employmentType: 'FullTime',
    workArrangement: 'Remote',
    cityId: null,
    cityName: null,
    provinceName: null,
    countryName: null,
    salaryMinimum: null,
    salaryMaximum: null,
    salaryCurrency: null,
    salaryPeriod: null,
    status: 'Draft',
    createdAt: '',
    updatedAt: null,
    publishedAt: null,
    closedAt: null,
    closingAt: null
  };
}
