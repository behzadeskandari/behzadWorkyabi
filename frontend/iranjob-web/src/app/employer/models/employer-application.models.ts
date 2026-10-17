export type ApplicationStatus =
  | 'Applied'
  | 'Withdrawn'
  | 'Reviewed'
  | 'Accepted'
  | 'Rejected';

export interface EmployerApplicationListItem {
  id: string;
  jobId: string;
  jobTitle: string;
  candidateProfileId: string;
  candidateHeadline: string | null;
  candidateCity: string | null;
  status: ApplicationStatus;
  appliedAt: string;
  updatedAt: string | null;
}

export interface EmployerApplicationDetail {
  id: string;
  jobId: string;
  jobTitle: string;
  jobDescription: string | null;
  candidateProfileId: string;
  candidateHeadline: string | null;
  candidateBiography: string | null;
  candidateCity: string | null;
  candidateProvince: string | null;
  candidateLinkedInUrl: string | null;
  candidateGitHubUrl: string | null;
  candidatePortfolioUrl: string | null;
  candidateExpectedSalary: number | null;
  candidateSalaryType: string | null;
  candidateEmploymentStatus: string | null;
  candidateAvailability: string | null;
  candidateMilitaryStatus: string | null;
  status: ApplicationStatus;
  appliedAt: string;
  updatedAt: string | null;
}

export interface EmployerResumeSectionView {
  id: string;
  primaryLabel: string | null;
  secondaryLabel: string | null;
  tertiaryLabel: string | null;
  startDate: string | null;
  endDate: string | null;
  isCurrent: boolean;
  durationMonths: number;
  sortOrder: number;
}

export interface EmployerResumeSkillView {
  id: string;
  skillId: string | null;
  skillName: string | null;
  proficiencyLevel: string | null;
  yearsOfExperience: number | null;
  sortOrder: number;
}

export interface EmployerResumeLanguageView {
  id: string;
  languageName: string | null;
  proficiencyLevel: string | null;
  sortOrder: number;
}

export interface EmployerResumeProjectView {
  id: string;
  title: string | null;
  description: string | null;
  url: string | null;
  startDate: string | null;
  endDate: string | null;
  isCurrent: boolean;
  sortOrder: number;
}

export interface EmployerResumeCertificationView {
  id: string;
  name: string | null;
  issuingOrganization: string | null;
  issueDate: string | null;
  expiryDate: string | null;
  certificateUrl: string | null;
  sortOrder: number;
}

export interface EmployerResumePortfolioLinkView {
  id: string;
  title: string | null;
  url: string | null;
  description: string | null;
  sortOrder: number;
}

export interface EmployerResumeViewResponse {
  applicationId: string;
  candidateProfileId: string;
  candidateHeadline: string | null;
  status: string;
  completenessScore: number;
  hasCvFile: boolean;
    workExperiences: EmployerResumeSectionView[];
  educations: EmployerResumeSectionView[];
  skills: EmployerResumeSkillView[];
  languages: EmployerResumeLanguageView[];
  projects: EmployerResumeProjectView[];
  certifications: EmployerResumeCertificationView[];
  portfolioLinks: EmployerResumePortfolioLinkView[];
}
