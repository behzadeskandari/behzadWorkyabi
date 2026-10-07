export type JobPostingStatus = 'Draft' | 'Published' | 'Closed';
export type EmploymentType = 'FullTime' | 'PartTime' | 'Contract' | 'Internship' | 'Temporary';
export type WorkArrangement = 'OnSite' | 'Hybrid' | 'Remote';
export type SalaryPeriod = 'Hourly' | 'Monthly' | 'Yearly';

export interface JobPostingReference {
  id: string;
  name: string;
}

export interface JobPosting {
  id: string;
  title: string;
  description: string;
  categoryId: string;
  categoryName: string;
  skills: JobPostingReference[];
  employmentType: EmploymentType;
  workArrangement: WorkArrangement;
  cityId: string | null;
  cityName: string | null;
  provinceName: string | null;
  countryName: string | null;
  salaryMinimum: number | null;
  salaryMaximum: number | null;
  salaryCurrency: string | null;
  salaryPeriod: SalaryPeriod | null;
  status: JobPostingStatus;
  createdAt: string;
  updatedAt: string | null;
  publishedAt: string | null;
  closedAt: string | null;
  closingAt: string | null;
}

export interface SaveJobPostingRequest {
  title: string;
  description: string;
  categoryId: string;
  skillIds: string[];
  employmentType: EmploymentType;
  workArrangement: WorkArrangement;
  cityId: string | null;
  salaryMinimum: number | null;
  salaryMaximum: number | null;
  salaryCurrency: string | null;
  salaryPeriod: SalaryPeriod | null;
  closingAt: string | null;
}
