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
