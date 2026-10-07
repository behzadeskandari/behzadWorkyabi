export interface EmployerProfile {
  id: string;
  userId: string;
  companyName: string;
  companyDescription: string | null;
  industry: string;
  companySize: string;
  websiteUrl: string | null;
  linkedInUrl: string | null;
  logoUrl: string | null;
  foundedYear: number | null;
  city: string;
  province: string;
  address: string | null;
  postalCode: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface SaveEmployerProfileRequest {
  companyName: string | null;
  companyDescription: string | null;
  industry: string | null;
  companySize: string | null;
  websiteUrl: string | null;
  linkedInUrl: string | null;
  logoUrl: string | null;
  foundedYear: number | null;
  city: string | null;
  province: string | null;
  address: string | null;
  postalCode: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
}

export const companySizeOptions: Array<{ value: string; label: string }> = [
  { value: '1-10', label: '۱ تا ۱۰ نفر' },
  { value: '11-50', label: '۱۱ تا ۵۰ نفر' },
  { value: '51-200', label: '۵۱ تا ۲۰۰ نفر' },
  { value: '201-500', label: '۲۰۱ تا ۵۰۰ نفر' },
  { value: '501-1000', label: '۵۰۱ تا ۱۰۰۰ نفر' },
  { value: '1000+', label: 'بیش از ۱۰۰۰ نفر' }
];

export const industryOptions: Array<{ value: string; label: string }> = [
  { value: 'InformationTechnology', label: 'فناوری اطلاعات' },
  { value: 'Finance', label: 'مالی و بانک' },
  { value: 'Healthcare', label: 'بهداشت و درمان' },
  { value: 'Manufacturing', label: 'تولید و ساخت' },
  { value: 'Education', label: 'آموزش' },
  { value: 'Retail', label: 'خرده‌فروشی' },
  { value: 'Construction', label: 'ساخت و ساز' },
  { value: 'Logistics', label: 'لجستیک و حمل‌ونقل' },
  { value: 'Automotive', label: 'خودرو' },
  { value: 'Marketing', label: 'بازاریابی و تبلیغات' },
  { value: 'Other', label: 'سایر' }
];
