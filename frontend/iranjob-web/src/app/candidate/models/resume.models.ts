export type ResumeStatus = 'Draft' | 'Complete' | 'ReadyToShare';

export interface ResumeSectionItem {
  id: string | null;
  primaryLabel: string | null;
  secondaryLabel: string | null;
  tertiaryLabel: string | null;
  startDate: string | null;
  endDate: string | null;
  isCurrent: boolean;
  durationMonths: number;
  sortOrder: number;
}

export interface ResumeSkillItem {
  id: string | null;
  skillId: string | null;
  skillName: string | null;
  proficiencyLevel: string | null;
  yearsOfExperience: number | null;
  sortOrder: number;
}

export interface ResumeLanguageItem {
  id: string | null;
  languageName: string | null;
  proficiencyLevel: string | null;
  sortOrder: number;
}

export interface SaveResumeRequest {
  professionalSummary: string | null;
  status: ResumeStatus | null;
  workExperiences: ResumeExperienceForm[] | null;
  educations: ResumeEducationForm[] | null;
  skills: ResumeSkillForm[] | null;
  languages: ResumeLanguageForm[] | null;
  projects: ResumeProjectForm[] | null;
  certifications: ResumeCertificationForm[] | null;
  portfolioLinks: ResumePortfolioLinkForm[] | null;
}

export interface ResumeExperienceForm {
  id: string | null;
  company: string;
  jobTitle: string;
  description: string | null;
  startDate: string | null;
  endDate: string | null;
  isCurrent: boolean;
}

export interface ResumeEducationForm {
  id: string | null;
  institution: string;
  degree: string;
  fieldOfStudy: string | null;
  grade: string | null;
  startDate: string | null;
  endDate: string | null;
  isCurrent: boolean;
}

export interface ResumeSkillForm {
  id: string | null;
  skillId: string | null;
  skillName: string;
  proficiencyLevel: string | null;
  yearsOfExperience: number | null;
}

export interface ResumeLanguageForm {
  id: string | null;
  languageName: string;
  proficiencyLevel: string;
}

export interface ResumeProjectForm {
  id: string | null;
  title: string;
  description: string | null;
  url: string | null;
  startDate: string | null;
  endDate: string | null;
  isCurrent: boolean;
}

export interface ResumeProjectItemDto {
  id: string;
  title: string | null;
  description: string | null;
  url: string | null;
  startDate: string | null;
  endDate: string | null;
  isCurrent: boolean;
  sortOrder: number;
}

export interface ResumeCertificationForm {
  id: string | null;
  name: string;
  issuingOrganization: string | null;
  issueDate: string | null;
  expiryDate: string | null;
  certificateUrl: string | null;
}

export interface ResumePortfolioLinkForm {
  id: string | null;
  title: string;
  url: string;
  description: string | null;
}

export interface ResumeResponseDto {
  id: string;
  userId: string;
  professionalSummary: string | null;
  status: ResumeStatus;
  completenessScore: number;
  hasCvFile: boolean;
  cvFileSizeBytes: number | null;
  cvUploadedAt: string | null;
  cvFileName: string | null;
  cvFileContentType: string | null;
  workExperiences: ResumeSectionItem[];
  educations: ResumeSectionItem[];
  skills: ResumeSkillItem[];
  languages: ResumeLanguageItem[];
  projects: ResumeProjectItemDto[];
  certifications: ResumeCertificationItemDto[];
  portfolioLinks: ResumePortfolioLinkItemDto[];
  createdAt: string;
  updatedAt: string;
}

export interface ResumeCertificationItemDto {
  id: string;
  name: string;
  issuingOrganization: string | null;
  issueDate: string | null;
  expiryDate: string | null;
  certificateUrl: string | null;
  sortOrder: number;
}

export interface ResumePortfolioLinkItemDto {
  id: string;
  title: string;
  url: string;
  description: string | null;
  sortOrder: number;
}
