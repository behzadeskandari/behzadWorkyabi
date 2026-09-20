export interface CandidateProfile {
  id: string;
  userId: string;
  headline: string | null;
  biography: string | null;
  dateOfBirth: string | null;
  gender: string | null;
  city: string | null;
  province: string | null;
  phone: string | null;
  email: string | null;
  linkedInUrl: string | null;
  gitHubUrl: string | null;
  portfolioUrl: string | null;
  expectedSalary: number | null;
  salaryType: string | null;
  employmentStatus: string | null;
  availability: string | null;
  militaryStatus: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface SaveCandidateProfileRequest {
  headline: string | null;
  biography: string | null;
  dateOfBirth: string | null;
  gender: string | null;
  city: string | null;
  province: string | null;
  phone: string | null;
  email: string | null;
  linkedInUrl: string | null;
  gitHubUrl: string | null;
  portfolioUrl: string | null;
  expectedSalary: number | null;
  salaryType: string | null;
  employmentStatus: string | null;
  availability: string | null;
  militaryStatus: string | null;
}

export const genderOptions: Array<{ value: string; label: string }> = [
  { value: 'Male', label: 'مرد' },
  { value: 'Female', label: 'زن' },
  { value: 'Other', label: 'سایر' }
];

export const salaryTypeOptions: Array<{ value: string; label: string }> = [
  { value: 'Monthly', label: 'ماهانه' },
  { value: 'Annual', label: 'سالانه' },
  { value: 'Hourly', label: 'ساعتی' }
];

export const employmentStatusOptions: Array<{ value: string; label: string }> = [
  { value: 'Employed', label: 'شاغل' },
  { value: 'Unemployed', label: 'جویای کار' },
  { value: 'Student', label: 'دانشجو' },
  { value: 'Freelancer', label: 'فریلنسر' }
];

export const availabilityOptions: Array<{ value: string; label: string }> = [
  { value: 'Immediate', label: 'بلافاصله' },
  { value: 'WithinTwoWeeks', label: 'تا دو هفته آینده' },
  { value: 'WithinOneMonth', label: 'تا یک ماه آینده' },
  { value: 'Negotiable', label: 'قابل مذاکره' }
];

export const militaryStatusOptions: Array<{ value: string; label: string }> = [
  { value: 'Completed', label: 'پایان خدمت' },
  { value: 'Exempt', label: 'معافیت دائم' },
  { value: 'Ongoing', label: 'در حال خدمت' },
  { value: 'Included', label: 'مشمول (کارتی)' }
];
