import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CandidateProfileService } from '../../candidate/services/candidate-profile.service';
import { CandidateProfileCompletionResponse } from '../../candidate/models/candidate-completion.models';
import { JalaliDateService } from '../../core/services/jalali-date.service';
import { CandidateResumeService } from '../services/candidate-resume.service';

interface MissingItem {
  key: string;
  label: string;
  editRoute: string;
}

@Component({
  selector: 'app-candidate-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './candidate-dashboard.component.html',
  styleUrls: ['./candidate-dashboard.component.scss']
})
export class CandidateDashboardComponent implements OnInit {
  private readonly profileService = inject(CandidateProfileService);
  private readonly resumeService = inject(CandidateResumeService);
  private readonly jalaliDate = inject(JalaliDateService);

  completion: CandidateProfileCompletionResponse | null = null;
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.profileService.getCompletion().subscribe({
      next: data => {
        this.completion = data;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'بارگذاری وضعیت تکمیل پروفایل ناموفق بود. دوباره تلاش کنید.';
      }
    });
  }

  get profileMissingItems(): MissingItem[] {
    if (!this.completion) return [];
    const map: Record<string, { label: string; editRoute: string }> = {
      Headline: { label: 'عنوان حرفه‌ای', editRoute: '/candidate/profile' },
      City: { label: 'شهر', editRoute: '/candidate/profile' },
      Province: { label: 'استان', editRoute: '/candidate/profile' },
      Biography: { label: 'بیوگرافی', editRoute: '/candidate/profile' },
      Gender: { label: 'جنسیت', editRoute: '/candidate/profile' },
      EmploymentStatus: { label: 'وضعیت اشتغال', editRoute: '/candidate/profile' },
      Availability: { label: 'در دسترس بودن', editRoute: '/candidate/profile' }
    };
    return this.completion.missingProfileFields
      .map(key => map[key] ?? { label: key, editRoute: '/candidate/profile' })
      .map((m, i) => ({ key: this.completion!.missingProfileFields[i], ...m }));
  }

  get resumeMissingItems(): MissingItem[] {
    if (!this.completion?.resume) return [];
    const map: Record<string, { label: string; editRoute: string }> = {
      ProfessionalSummary: { label: 'خلاصهٔ حرفه‌ای', editRoute: '/candidate/resume' },
      WorkExperiences: { label: 'تجربهٔ کاری', editRoute: '/candidate/resume' },
      Educations: { label: 'تحصیلات', editRoute: '/candidate/resume' },
      Skills: { label: 'مهارت‌ها', editRoute: '/candidate/resume' },
      Languages: { label: 'زبان‌ها', editRoute: '/candidate/resume' },
      Projects: { label: 'پروژه‌ها', editRoute: '/candidate/resume' },
      Certifications: { label: 'گواهینامه‌ها', editRoute: '/candidate/resume' }
    };
    return this.completion.resume.missingSections
      .map(key => map[key] ?? { label: key, editRoute: '/candidate/resume' });
  }

  get profilePct(): number {
    return this.completion?.profileCompletenessScore ?? 0;
  }

  get resumePct(): number {
    return this.completion?.resume?.completenessScore ?? 0;
  }

  get canPreviewResume(): boolean {
    return !!this.completion?.resume?.hasResume;
  }

  formatDate(value: string | null): string {
    if (!value) return '—';
    return this.jalaliDate.formatDisplay(value);
  }
}
