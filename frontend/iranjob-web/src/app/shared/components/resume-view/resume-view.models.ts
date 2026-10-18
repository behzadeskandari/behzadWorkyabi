/// <summary>
/// Normalized presentation model consumed by <see cref="ResumeViewComponent" />.
/// The employer-facing <c>EmployerResumeViewResponse</c> and the candidate-facing
/// <c>ResumeResponseDto</c> both satisfy this structural contract, so a single
/// read-only viewer can render either source without coupling the component to
/// either DTO shape.
/// </summary>
export interface ResumePresentation {
  professionalSummary: string | null;
  workExperiences: ResumeSectionPresentation[] | null;
  educations: ResumeSectionPresentation[] | null;
  skills: ResumeSkillPresentation[] | null;
  languages: ResumeLanguagePresentation[] | null;
  projects: ResumeProjectPresentation[] | null;
  certifications: ResumeCertificationPresentation[] | null;
  portfolioLinks: ResumePortfolioPresentation[] | null;
}

export interface ResumeSectionPresentation {
  primaryLabel: string | null;
  secondaryLabel: string | null;
  tertiaryLabel: string | null;
  startDate: string | null;
  endDate: string | null;
  isCurrent: boolean;
  durationMonths: number;
  sortOrder: number;
}

export interface ResumeSkillPresentation {
  skillName: string | null;
  proficiencyLevel: string | null;
  yearsOfExperience: number | null;
  sortOrder: number;
}

export interface ResumeLanguagePresentation {
  languageName: string | null;
  proficiencyLevel: string | null;
  sortOrder: number;
}

export interface ResumeProjectPresentation {
  title: string | null;
  description: string | null;
  url: string | null;
  startDate: string | null;
  endDate: string | null;
  isCurrent: boolean;
  sortOrder: number;
}

export interface ResumeCertificationPresentation {
  name: string | null;
  issuingOrganization: string | null;
  issueDate: string | null;
  expiryDate: string | null;
  certificateUrl: string | null;
  sortOrder: number;
}

export interface ResumePortfolioPresentation {
  title: string | null;
  url: string | null;
  description: string | null;
  sortOrder: number;
}
