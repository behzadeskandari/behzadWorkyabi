// import { ComponentFixture, TestBed } from '@angular/core/testing';
// import { ResumeViewComponent } from './resume-view.component';
// import { EmployerResumeViewResponse } from '../../../employer/models/employer-application.models';
// import { JalaliDateService } from '../../../core/services/jalali-date.service';

// describe('ResumeViewComponent', () => {
//   let fixture: ComponentFixture<ResumeViewComponent>;
//   let component: ResumeViewComponent;

//   const emptyResume: EmployerResumeViewResponse = {
//     applicationId: 'app-1',
//     candidateProfileId: 'cand-1',
//     candidateHeadline: 'Frontend Engineer',
//     status: 'ReadyToShare',
//     completenessScore: 100,
//     hasCvFile: true,
//     workExperiences: [],
//     educations: [],
//     skills: [],
//     languages: [],
//     projects: [],
//     certifications: [],
//     portfolioLinks: []
//   };

//   beforeEach(() => {
//     TestBed.configureTestingModule({
//       imports: [ResumeViewComponent],
//       providers: [JalaliDateService]
//     });
//     fixture = TestBed.createComponent(ResumeViewComponent);
//     component = fixture.componentInstance;
//     component.resume = { ...emptyResume };
//   });

//   it('should create', () => {
//     fixture.detectChanges();
//     expect(component).toBeTruthy();
//   });

//   it('should render the Jalali date range for a current work experience', () => {
//     const full: EmployerResumeViewResponse = {
//       ...emptyResume,
//       workExperiences: [{
//         id: 'we-1', primaryLabel: 'Acme', secondaryLabel: 'Software Engineer',
//         tertiaryLabel: 'Built stuff.', startDate: '2023-01-15T00:00:00Z',
//         endDate: null, isCurrent: true, durationMonths: 24, sortOrder: 0
//       }]
//     };
//     component.resume = full;
//     fixture.detectChanges();

//     const label = component.dateRangeLabel(full.workExperiences[0]);
//     // 2023-01-15 → Jalali 1401/10/25; "اکنون" for current end
//     expect(label).toContain('1401');
//     expect(label).toContain('اکنون');
//   });

//     it('should render the Jalali date range for a completed work experience', () => {
//     const full: EmployerResumeViewResponse = {
//       ...emptyResume,
//       workExperiences: [{
//         id: 'we-2', primaryLabel: 'Acme', secondaryLabel: 'Software Engineer',
//         tertiaryLabel: null, startDate: '2022-03-10T00:00:00Z',
//         endDate: '2023-08-20T00:00:00Z', isCurrent: false, durationMonths: 17, sortOrder: 0
//       }]
//     };
//     const label = component.dateRangeLabel(full.workExperiences[0]);
//     // 2022-03-10 → Jalali 1400/12/19; 2023-08-20 → Jalali 1402/05/29
//     expect(label).toBe('1400/12/19 - 1402/05/29');
//     expect(label).not.toContain('اکنون');
//   });

//   it('should format a single date with Jalali digits', () => {
//     // 2024-01-01 Gregorian → Jalali 1402/10/11
//     const formatted = component.formatDate(new Date('2024-01-01T00:00:00Z').toISOString());
//     expect(formatted).toContain('1402');
//   });

//   it('should render the empty state when no work experiences exist', () => {
//     fixture.detectChanges();
//     const content = fixture.nativeElement.textContent as string;
//     expect(content).toContain('بدون سابقه کاری.');
//   });

//   it('should produce "کمتر از یک ماه" for a zero-month duration', () => {
//     expect(component.durationLabel(0)).toBe('کمتر از یک ماه');
//   });

//   it('should compose years and months for a longer duration', () => {
//     expect(component.durationLabel(27)).toBe('2 سال و 3 ماه');
//   });

//   it('should return the fallback dash when a date is null', () => {
//     expect(component.formatDate(null)).toBe('—');
//   });
// });
