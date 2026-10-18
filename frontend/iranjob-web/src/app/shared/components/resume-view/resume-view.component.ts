import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { JalaliDateService } from '../../../core/services/jalali-date.service';
import type {
  ResumePresentation,
  ResumeSectionPresentation,
  ResumeSkillPresentation,
  ResumeLanguagePresentation,
  ResumeProjectPresentation,
  ResumeCertificationPresentation,
  ResumePortfolioPresentation
} from './resume-view.models';

@Component({
  selector: 'app-resume-view',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './resume-view.component.html',
  styleUrls: ['./resume-view.component.scss']
})
export class ResumeViewComponent {
  /** Normalized resume presentation; accepts EmployerResumeViewResponse or ResumeResponseDto. */
  @Input({ required: true }) resume!: ResumePresentation;

  private readonly jalaliDate = inject(JalaliDateService);

    // Convenience accessors for the template.
  get professionalSummary(): string | null { return this.resume.professionalSummary ?? null; }
  get workExperiences(): ResumeSectionPresentation[] { return this.resume.workExperiences ?? []; }
  get educations(): ResumeSectionPresentation[] { return this.resume.educations ?? []; }
  get skills(): ResumeSkillPresentation[] { return (this.resume.skills ?? []) as ResumeSkillPresentation[]; }
  get languages(): ResumeLanguagePresentation[] { return (this.resume.languages ?? []) as ResumeLanguagePresentation[]; }
  get projects(): ResumeProjectPresentation[] { return (this.resume.projects ?? []) as ResumeProjectPresentation[]; }
  get certifications(): ResumeCertificationPresentation[] { return this.resume.certifications ?? []; }
  get portfolioLinks(): ResumePortfolioPresentation[] { return (this.resume.portfolioLinks ?? []) as ResumePortfolioPresentation[]; }

  sectionLabel(section: ResumeSectionPresentation): string {
    return section.primaryLabel ?? section.secondaryLabel ?? section.tertiaryLabel ?? '—';
  }

  sectionSecondary(section: ResumeSectionPresentation): string | null {
    return section.secondaryLabel;
  }

  sectionTertiary(section: ResumeSectionPresentation): string | null {
    return section.tertiaryLabel;
  }

  durationLabel(months: number): string {
    if (months <= 0) return 'کمتر از یک ماه';
    const years = Math.floor(months / 12);
    const remainder = months % 12;
    const parts: string[] = [];
    if (years > 0) parts.push(`${years} سال`);
    if (remainder > 0) parts.push(`${remainder} ماه`);
    return parts.join(' و ');
  }

    dateRangeLabel(section: ResumeSectionPresentation | ResumeProjectPresentation): string {
    const fmt = (value: string | null) => value ? this.jalaliDate.toJalaliString(new Date(value)) : null;
    const start = fmt(section.startDate);
    const end = section.isCurrent ? 'اکنون' : fmt(section.endDate);
    return `${start ?? '—'} - ${end ?? '—'}`;
  }
    formatDate(value: string | null): string {
    return value ? this.jalaliDate.toJalaliString(new Date(value)) : '—';
  }

  certificationDateLabel(cert: ResumeCertificationPresentation): string {
    const issued = cert.issueDate ? this.jalaliDate.toJalaliString(new Date(cert.issueDate)) : '';
    const parts = issued ? [`صادر: ${issued}`] : [];
    if (cert.expiryDate) parts.push(`منقضی: ${this.jalaliDate.toJalaliString(new Date(cert.expiryDate))}`);
    return parts.join('، ') || '—';
  }
}

