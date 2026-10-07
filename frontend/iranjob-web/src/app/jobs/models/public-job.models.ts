export type PublicJobSort = 'Newest' | 'Oldest' | 'SalaryAscending' | 'SalaryDescending';

export interface PublicJobSearchQuery {
  q?: string;
  categoryId?: string;
  skillIds?: string[];
  countryId?: string;
  provinceId?: string;
  cityId?: string;
  workArrangement?: string;
  employmentType?: string;
  salaryMinimum?: number;
  salaryMaximum?: number;
  salaryCurrency?: string;
  page?: number;
  pageSize?: number;
  sort?: PublicJobSort;
}

export interface PublicJobSearchPage {
  items: PublicJobSearchItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PublicJobSearchItem {
  id: string;
  title: string;
  companyName: string;
  companyLogoUrl: string | null;
  category: string;
  description: string;
  skills: string[];
  city: string;
  province: string;
  country: string;
  employmentType: string;
  workArrangement: string;
  salaryMinimum: number | null;
  salaryMaximum: number | null;
  salaryCurrency: string | null;
  salaryPeriod: string | null;
  publishedAt: string;
}

export interface PublicJobDetails {
  id: string;
  title: string;
  description: string;
  category: string;
  skills: string[];
  city: string;
  province: string;
  country: string;
  employmentType: string;
  workArrangement: string;
  salaryMinimum: number | null;
  salaryMaximum: number | null;
  salaryCurrency: string | null;
  salaryPeriod: string | null;
  publishedAt: string;
  employer: {
    companyName: string;
    description: string | null;
    industry: string;
    websiteUrl: string | null;
    logoUrl: string | null;
  };
}
