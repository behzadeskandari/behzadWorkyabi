export type ResumeStatus = 'Draft' | 'Complete' | 'ReadyToShare';

export interface ResumeCompletionResponse {
  completenessScore: number;
  hasResume: boolean;
  status: ResumeStatus;
  isShared: boolean;
  hasCvFile: boolean;
  missingSections: string[];
}

export interface ApplicationReadiness {
  isReady: boolean;
  hasRequiredProfile: boolean;
  hasRecommendedResume: boolean;
  requiredMissing: string[];
  recommendedMissing: string[];
  message: string;
}

export interface CandidateProfileCompletionResponse {
  profileCompletenessScore: number;
  hasProfile: boolean;
  missingProfileFields: string[];
  resume: ResumeCompletionResponse | null;
  readiness: ApplicationReadiness;
}
