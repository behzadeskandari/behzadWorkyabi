export interface SavedJobItem { jobPostingId: string; title: string; companyName: string; category: string; city: string; savedAt: string; isAvailable: boolean; }
export interface SavedJobPage { items: SavedJobItem[]; page: number; pageSize: number; totalCount: number; }
export interface JobAlertRequest { name: string; categoryId: string | null; cityId: string | null; workArrangement: string | null; skillIds: string[]; isEnabled: boolean; }
export interface JobAlert extends JobAlertRequest { id: string; createdAt: string; updatedAt: string; }
