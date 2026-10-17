import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  EmployerResumeViewResponse,
  EmployerResumeSectionView,
  EmployerResumeSkillView,
  EmployerResumeLanguageView,
  EmployerResumeProjectView,
  EmployerResumeCertificationView,
  EmployerResumePortfolioLinkView
} from '../../../employer/models/employer-application.models';
import { JalaliDateService } from '../../../core/services/jalali-date.service';

@Component({
  selector: 'app-resume-view',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './resume-view.component.html',
  styleUrls: ['./resume-view.component.scss']
})
export class ResumeViewComponent {
  @Input({ required: true }) resume!: EmployerResumeViewResponse;

  private readonly jalaliDate = inject(JalaliDateService);

    // Convenience accessors for the template.
  get workExperiences(): EmployerResumeSectionView[] { return this.resume.workExperiences ?? []; }
  get educations(): EmployerResumeSectionView[] { return this.resume.educations ?? []; }
  get skills(): EmployerResumeSkillView[] { return this.resume.skills ?? []; }
  get languages(): EmployerResumeLanguageView[] { return this.resume.languages ?? []; }
  get projects(): EmployerResumeProjectView[] { return this.resume.projects ?? []; }
  get certifications(): EmployerResumeCertificationView[] { return this.resume.certifications ?? []; }
  get portfolioLinks(): EmployerResumePortfolioLinkView[] { return this.resume.portfolioLinks ?? []; }

  sectionLabel(section: EmployerResumeSectionView): string {
    return section.primaryLabel ?? section.secondaryLabel ?? section.tertiaryLabel ?? '—';
  }

  sectionSecondary(section: EmployerResumeSectionView): string | null {
    return section.secondaryLabel;
  }

  sectionTertiary(section: EmployerResumeSectionView): string | null {
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

    dateRangeLabel(section: EmployerResumeSectionView | EmployerResumeProjectView): string {
    const fmt = (value: string | null) => value ? this.jalaliDate.toJalaliString(new Date(value)) : null;
    const start = fmt(section.startDate);
    const end = section.isCurrent ? 'اکنون' : fmt(section.endDate);
    return `${start ?? '—'} - ${end ?? '—'}`;
  }
    formatDate(value: string | null): string {
    return value ? this.jalaliDate.toJalaliString(new Date(value)) : '—';
  }

  certificationDateLabel(cert: EmployerResumeCertificationView): string {
    const issued = cert.issueDate ? this.jalaliDate.toJalaliString(new Date(cert.issueDate)) : '';
    const parts = issued ? [`صادر: ${issued}`] : [];
    if (cert.expiryDate) parts.push(`منقضی: ${this.jalaliDate.toJalaliString(new Date(cert.expiryDate))}`);
    return parts.join('، ') || '—';
  }
}

