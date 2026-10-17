// import { ComponentFixture, TestBed } from '@angular/core/testing';
// import { ReactiveFormsModule, FormArray } from '@angular/forms';
// import { of } from 'rxjs';
// import { CandidateResumeComponent } from './resume.component';
// import { CandidateResumeService } from '../../services/candidate-resume.service';
// import { ResumeResponseDto } from '../../models/resume.models';
// import { Skill } from '../../../reference-data/models/reference-data.models';

// describe('CandidateResumeComponent', () => {
//   let fixture: ComponentFixture<CandidateResumeComponent>;
//   let component: CandidateResumeComponent;
//   let resumeService: jasmine.SpyObj<CandidateResumeService>;

//     const mockSkills: Skill[] = [
//     { id: 'skill-1', name: 'Angular', description: null, isActive: true, createdAt: '2026-01-01T00:00:00Z', updatedAt: null },
//     { id: 'skill-2', name: 'TypeScript', description: null, isActive: true, createdAt: '2026-01-01T00:00:00Z', updatedAt: null },
//   ];

//   const mockResume: ResumeResponseDto = {
//     id: 'resume-1', userId: 'user-1', professionalSummary: 'Test summary',
//     status: 'Draft', completenessScore: 50, hasCvFile: false,
//     cvFileSizeBytes: null, cvUploadedAt: null, cvFileName: null,
//     cvFileContentType: null, workExperiences: [], educations: [],
//     skills: [], languages: [], projects: [], certifications: [],
//     portfolioLinks: [], createdAt: '2026-01-01T00:00:00Z',
//     updatedAt: '2026-01-01T00:00:00Z',
//   };

//   beforeEach(async () => {
//     resumeService = jasmine.createSpyObj('CandidateResumeService', [
//       'getResume', 'saveResume', 'setSharingEnabled', 'getSkills', 'uploadCv',
//     ]);
//     resumeService.getResume.and.returnValue(of(mockResume));
//     resumeService.getSkills.and.returnValue(of(mockSkills));
//     await TestBed.configureTestingModule({
//       imports: [ReactiveFormsModule, CandidateResumeComponent],
//       providers: [{ provide: CandidateResumeService, useValue: resumeService }],
//     }).compileComponents();
//   });

//   it('should create the component', () => {
//     fixture = TestBed.createComponent(CandidateResumeComponent);
//     component = fixture.componentInstance;
//     expect(component).toBeTruthy();
//   });

//   it('should initialize all FormArray getters with correct counts', () => {
//     fixture = TestBed.createComponent(CandidateResumeComponent);
//     component = fixture.componentInstance;
//          expect(component.workExperienceArray.controls.length).toBe(0);
//      expect(component.educationArray.controls.length).toBe(0);
//      expect(component.skillArray.controls.length).toBe(0);
//      expect(component.languageArray.controls.length).toBe(0);
//      expect(component.projectArray.controls.length).toBe(0);
//      expect(component.certificationArray.controls.length).toBe(0);
//      expect(component.portfolioArray.controls.length).toBe(0);
//   });

//     it('should return FormArray type from all getters', () => {
//     fixture = TestBed.createComponent(CandidateResumeComponent);
//     component = fixture.componentInstance;
//     expect(component.workExperienceArray).toBeInstanceOf(FormArray);
//     expect(component.educationArray).toBeInstanceOf(FormArray);
//     expect(component.skillArray).toBeInstanceOf(FormArray);
//     expect(component.languageArray).toBeInstanceOf(FormArray);
//     expect(component.projectArray).toBeInstanceOf(FormArray);
//     expect(component.certificationArray).toBeInstanceOf(FormArray);
//     expect(component.portfolioArray).toBeInstanceOf(FormArray);
//   });

//   describe('add/remove operations', () => {
//     beforeEach(() => {
//       fixture = TestBed.createComponent(CandidateResumeComponent);
//       component = fixture.componentInstance;
//     });

//     it('should add a work experience', () => {
//       component.addWorkExperience();
//             expect(component.workExperienceArray.controls.length).toBe(1);
//     });

//     it('should remove a work experience using removeAt', () => {
//       component.addWorkExperience();
//       component.removeWorkExperience(0);
//             expect(component.workExperienceArray.controls.length).toBe(0);
//     });

//     it('should add and remove an education', () => {
//       component.addEducation();
//             expect(component.educationArray.controls.length).toBe(1);
//       component.removeEducation(0);
//             expect(component.educationArray.controls.length).toBe(0);
//     });

//     it('should add and remove a language', () => {
//       component.addLanguage();
//             expect(component.languageArray.controls.length).toBe(1);
//       component.removeLanguage(0);
//             expect(component.languageArray.controls.length).toBe(0);
//     });

//     it('should add and remove a project', () => {
//       component.addProject();
//             expect(component.projectArray.controls.length).toBe(1);
//       component.removeProject(0);
//             expect(component.projectArray.controls.length).toBe(0);
//     });

//     it('should add and remove a certification', () => {
//       component.addCertification();
//             expect(component.certificationArray.controls.length).toBe(1);
//       component.removeCertification(0);
//             expect(component.certificationArray.controls.length).toBe(0);
//     });

//     it('should add and remove a portfolio link', () => {
//       component.addPortfolio();
//             expect(component.portfolioArray.controls.length).toBe(1);
//       component.removePortfolio(0);
//             expect(component.portfolioArray.controls.length).toBe(0);
//     });
//   });

//   describe('onStatusChange', () => {
//     it('should call setSharing(true) when status is ReadyToShare', () => {
//       fixture = TestBed.createComponent(CandidateResumeComponent);
//       component = fixture.componentInstance;
//       spyOn(component, 'setSharing');
//       const event = { target: { value: 'ReadyToShare' } } as unknown as Event;
//       component.onStatusChange(event);
//       expect(component.setSharing).toHaveBeenCalledWith(true);
//     });

//     it('should call setSharing(false) when status is Draft', () => {
//       fixture = TestBed.createComponent(CandidateResumeComponent);
//       component = fixture.componentInstance;
//       spyOn(component, 'setSharing');
//       const event = { target: { value: 'Draft' } } as unknown as Event;
//       component.onStatusChange(event);
//       expect(component.setSharing).toHaveBeenCalledWith(false);
//     });

//     it('should call setSharing(false) when status is Complete', () => {
//       fixture = TestBed.createComponent(CandidateResumeComponent);
//       component = fixture.componentInstance;
//       spyOn(component, 'setSharing');
//       const event = { target: { value: 'Complete' } } as unknown as Event;
//       component.onStatusChange(event);
//       expect(component.setSharing).toHaveBeenCalledWith(false);
//     });
//   });

//   it('should have statusOptions with Draft, Complete, ReadyToShare', () => {
//     fixture = TestBed.createComponent(CandidateResumeComponent);
//     component = fixture.componentInstance;
//         expect(component.statusOptions.length).toBe(3);
//     expect(component.statusOptions[0].value).toBe('Draft');
//     expect(component.statusOptions[1].value).toBe('Complete');
//     expect(component.statusOptions[2].value).toBe('ReadyToShare');
//   });
// });
