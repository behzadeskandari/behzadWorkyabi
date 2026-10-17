import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators, FormArray } from '@angular/forms';
import { JalaliDatepickerComponent } from '../../../core/components/jalali-datepicker.component';
import { SkillSelectComponent } from '../../../reference-data/components/skill-select.component';
import { CandidateResumeService } from '../../services/candidate-resume.service';
import { ResumeViewComponent } from '../../../shared/components/resume-view/resume-view.component';
import {
  SaveResumeRequest, ResumeResponseDto
} from '../../models/resume.models';
import { Skill } from '../../../reference-data/models/reference-data.models';

@Component({
  selector: 'app-candidate-resume',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, JalaliDatepickerComponent, SkillSelectComponent, ResumeViewComponent],
  templateUrl: './resume.component.html',
  styleUrls: ['./resume.component.scss']
})
export class CandidateResumeComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly resumeService = inject(CandidateResumeService);

  form = this.fb.group({
    professionalSummary: ['', [Validators.maxLength(2000)]],
        workExperiences: this.fb.array([], { validators: [Validators.maxLength(50)] }),
    educations: this.fb.array([], { validators: [Validators.maxLength(50)] }),
    skills: this.fb.array([], { validators: [Validators.maxLength(50)] }),
    languages: this.fb.array([], { validators: [Validators.maxLength(10)] }),
    projects: this.fb.array([], { validators: [Validators.maxLength(50)] }),
    certifications: this.fb.array([], { validators: [Validators.maxLength(50)] }),
    portfolioLinks: this.fb.array([], { validators: [Validators.maxLength(20)] })
  });

    existingResume: ResumeResponseDto | null = null;
  selectedSkillIds: string[] = [];
  isLoading = false;
  isSharing = false;
  isSaving = false;
  isUploadingCv = false;
  errorMessage = '';
  successMessage = '';
  validationErrors: Record<string, string[]> = {};
  skills: Skill[] = [];

  readonly statusOptions = [
    { value: 'Draft', label: 'پیش‌نویس' },
    { value: 'Complete', label: 'کامل' },
    { value: 'ReadyToShare', label: 'آمادهٔ اشتراک‌گذاری' }
  ];

    get workExperienceArray() { return this.form.get('workExperiences') as FormArray; }
  get educationArray() { return this.form.get('educations') as FormArray; }
  get skillArray() { return this.form.get('skills') as FormArray; }
  get languageArray() { return this.form.get('languages') as FormArray; }
  get projectArray() { return this.form.get('projects') as FormArray; }
  get certificationArray() { return this.form.get('certifications') as FormArray; }
  get portfolioArray() { return this.form.get('portfolioLinks') as FormArray; }

  constructor() {}

  ngOnInit(): void { this.loadResume(); }

  loadResume(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.resumeService.getResume().subscribe({
      next: resume => { this.existingResume = resume; this.patchResume(resume); },
      error: err => { if (err.status !== 404) this.errorMessage = 'رزومه بارگذاری نشد.'; },
      complete: () => (this.isLoading = false)
    });
  }

  private patchResume(resume: ResumeResponseDto): void {
    this.workExperienceArray.clear();
    resume.workExperiences.forEach(e => this.workExperienceArray.push(this.fb.group({
      id: [e.id], company: [e.primaryLabel, Validators.required], jobTitle: [e.secondaryLabel, Validators.required],
      description: [e.tertiaryLabel], startDate: [e.startDate], endDate: [e.endDate], isCurrent: [e.isCurrent]
    })));
    this.educationArray.clear();
    resume.educations.forEach(e => this.educationArray.push(this.fb.group({
      id: [e.id], institution: [e.primaryLabel, Validators.required], degree: [e.secondaryLabel, Validators.required],
      fieldOfStudy: [e.tertiaryLabel], startDate: [e.startDate], endDate: [e.endDate], isCurrent: [e.isCurrent]
    })));
        this.skillArray.clear();
    resume.skills.forEach(s => {
      this.skillArray.push(this.fb.group({
        id: [s.id], skillId: [s.skillId], skillName: [s.skillName, Validators.required],
        proficiencyLevel: [s.proficiencyLevel], yearsOfExperience: [s.yearsOfExperience]
      }));
      if (s.skillId) this.selectedSkillIds.push(s.skillId);
    });
    this.languageArray.clear();
    resume.languages.forEach(l => this.languageArray.push(this.fb.group({
      id: [l.id], languageName: [l.languageName, Validators.required], proficiencyLevel: [l.proficiencyLevel, Validators.required]
    })));
    this.projectArray.clear();
    resume.projects.forEach(p => this.projectArray.push(this.fb.group({
      id: [p.id], title: [p.primaryLabel, Validators.required], description: [p.secondaryLabel],
      url: [p.tertiaryLabel], startDate: [p.startDate], endDate: [p.endDate], isCurrent: [p.isCurrent]
    })));
    this.certificationArray.clear();
    resume.certifications.forEach(c => this.certificationArray.push(this.fb.group({
      id: [c.id], name: [c.name, Validators.required], issuingOrganization: [c.issuingOrganization],
      issueDate: [c.issueDate], expiryDate: [c.expiryDate], certificateUrl: [c.certificateUrl]
    })));
    this.portfolioArray.clear();
    resume.portfolioLinks.forEach(l => this.portfolioArray.push(this.fb.group({
      id: [l.id], title: [l.title, Validators.required], url: [l.url, Validators.required], description: [l.description]
    })));
  }

  trackByIndex(i: number): number { return i; }
  addWorkExperience(): void { this.workExperienceArray.push(this.newWorkExperience()); }
  addEducation(): void { this.educationArray.push(this.newEducation()); }
  addLanguage(): void { this.languageArray.push(this.newLanguage()); }
  addProject(): void { this.projectArray.push(this.newProject()); }
  addCertification(): void { this.certificationArray.push(this.newCertification()); }
  addPortfolio(): void { this.portfolioArray.push(this.newPortfolio()); }
    removeWorkExperience(i: number): void { this.workExperienceArray.removeAt(i); }
  removeEducation(i: number): void { this.educationArray.removeAt(i); }
  removeLanguage(i: number): void { this.languageArray.removeAt(i); }
  removeProject(i: number): void { this.projectArray.removeAt(i); }
  removeCertification(i: number): void { this.certificationArray.removeAt(i); }
  removePortfolio(i: number): void { this.portfolioArray.removeAt(i); }

    selectSkillIds(ids: string[]): void {
    this.selectedSkillIds = ids;
    this.skillArray.clear();
    ids.forEach(id => {
      const skill = this.skills.find(s => s.id === id);
      if (skill) {
        this.skillArray.push(this.fb.group({
          id: [null], skillId: [skill.id], skillName: [skill.name],
          proficiencyLevel: [''], yearsOfExperience: [null]
        }));
      }
    });
  }

  removeSkillAt(index: number): void {
    const skillId = this.skillArray.at(index)?.get('skillId')?.value;
    if (skillId) {
      this.selectedSkillIds = this.selectedSkillIds.filter(id => id !== skillId);
    }
        this.skillArray.removeAt(index);
  }

  newWorkExperience() { return this.fb.group({ id: [null], company: ['', Validators.required], jobTitle: ['', Validators.required], description: ['', Validators.maxLength(2000)], startDate: [null], endDate: [null], isCurrent: [false] }); }
  newEducation() { return this.fb.group({ id: [null], institution: ['', Validators.required], degree: ['', Validators.required], fieldOfStudy: [''], startDate: [null], endDate: [null], isCurrent: [false] }); }
  newLanguage() { return this.fb.group({ id: [null], languageName: ['', Validators.required], proficiencyLevel: ['', Validators.required] }); }
  newProject() { return this.fb.group({ id: [null], title: ['', Validators.required], description: ['', Validators.maxLength(4000)], url: ['', Validators.maxLength(500)], startDate: [null], endDate: [null], isCurrent: [false] }); }
  newCertification() { return this.fb.group({ id: [null], name: ['', Validators.required], issuingOrganization: [''], issueDate: [null], expiryDate: [null], certificateUrl: [''] }); }
  newPortfolio() { return this.fb.group({ id: [null], title: ['', Validators.required], url: ['', Validators.required], description: [''] }); }

  onStartDateChange(index: number, section: 'work' | 'education' | 'project', value: string | null): void {
    const control = section === 'work' ? this.workExperienceArray.at(index) : section === 'education' ? this.educationArray.at(index) : this.projectArray.at(index);
    control?.get('startDate')?.setValue(value, { emitModelToViewChange: false });
  }
  onEndDateChange(index: number, section: 'work' | 'education' | 'project', value: string | null): void {
    const control = section === 'work' ? this.workExperienceArray.at(index) : section === 'education' ? this.educationArray.at(index) : this.projectArray.at(index);
    control?.get('endDate')?.setValue(value, { emitModelToViewChange: false });
  }

    get isShared(): boolean { return this.existingResume?.status === 'ReadyToShare' && !this.isSharing; }

  save(): void {
    if (this.form.invalid) return;
    this.isSaving = true;
    this.successMessage = '';
    this.errorMessage = '';
    this.validationErrors = {};
    const request: SaveResumeRequest = this.buildRequest();
    this.resumeService.saveResume(request).subscribe({
      next: saved => {
        this.existingResume = saved;
        this.successMessage = 'رزومه با موفقیت ذخیره شد.';
        this.loadResume();
      },
      error: err => {
        if (err.status === 400 && err.error?.errors) {
          this.validationErrors = err.error.errors;
        } else {
          this.errorMessage = 'ذخیرهٔ رزومه ناموفق بود.';
        }
      },
      complete: () => (this.isSaving = false)
    });
  }

    onStatusChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const status = select.value as 'Draft' | 'Complete' | 'ReadyToShare';
    if (status === 'ReadyToShare') {
      this.setSharing(true);
    } else {
      this.setSharing(false);
    }
  }

  setSharing(share: boolean): void {
    this.isSharing = true;
    this.errorMessage = '';
    this.resumeService.setSharingEnabled(share).subscribe({
      next: saved => {
        this.existingResume = saved;
        this.successMessage = share ? 'رزومه به اشتراک گذاشته شد.' : 'اشتراک‌گذاری لغو شد.';
      },
      error: () => { this.errorMessage = 'بروزرسانی وضعیت ناموفق بود.'; },
      complete: () => (this.isSharing = false)
    });
  }

  uploadCv(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    if (!this.isAllowedCv(file)) {
      this.errorMessage = 'فقط فایل‌های PDF یا Word (حداکثر ۵ مگابایت) مجاز است.';
      input.value = '';
      return;
    }
    this.isUploadingCv = true;
    this.resumeService.uploadCv(file).subscribe({
      next: saved => { this.existingResume = saved; this.successMessage = 'فایل رزومه آپلود شد.'; },
      error: () => { this.errorMessage = 'آپلود فایل رزومه ناموفق بود.'; },
      complete: () => { this.isUploadingCv = false; input.value = ''; }
    });
  }

  private isAllowedCv(file: File): boolean {
    const allowedExts = ['.pdf', '.doc', '.docx'];
    const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
    return file.size <= 5 * 1024 * 1024 && allowedExts.includes(ext);
  }

  private buildRequest(): SaveResumeRequest {
    return {
      professionalSummary: this.form.controls.professionalSummary.value,
      status: this.existingResume?.status ?? 'Draft',
      workExperiences: this.workExperienceArray.controls.map(c => c.getRawValue()),
      educations: this.educationArray.controls.map(c => c.getRawValue()),
      languages: this.languageArray.controls.map(c => c.getRawValue()),
      projects: this.projectArray.controls.map(c => c.getRawValue()),
      certifications: this.certificationArray.controls.map(c => c.getRawValue()),
      portfolioLinks: this.portfolioArray.controls.map(c => c.getRawValue()),
      skills: this.skillArray.controls.map(c => {
        const v = c.getRawValue();
        return { ...v, skillName: v.skillName ?? this.skills.find(s => s.id === v.skillId)?.name ?? '' };
      })
    };
  }
}
