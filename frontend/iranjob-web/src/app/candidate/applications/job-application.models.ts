export interface JobApplication {
  id: string;
  jobId: string;
  jobTitle: string;
  companyName: string;
  status: 'Applied' | 'Withdrawn';
  appliedAt: string;
  updatedAt: string | null;
}
