import { CommonModule } from '@angular/common';
import { Component, DestroyRef, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Skill } from '../models/reference-data.models';
import { SkillService } from '../services/skill.service';

@Component({
  selector: 'app-skill-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './skill-select.component.html',
  styleUrl: './skill-select.component.scss'
})
export class SkillSelectComponent implements OnInit {
  @Input() valueIds: string[] = [];
  @Input() label = 'مهارت‌ها';
  @Output() readonly valueIdsChange = new EventEmitter<string[]>();

  skills: Skill[] = [];
  visibleSkills: Skill[] = [];
  searchTerm = '';
  isLoading = false;
  errorMessage = '';

  private readonly destroyRef = inject(DestroyRef);
  private readonly skillService = inject(SkillService);

  ngOnInit(): void {
    this.loadSkills();
  }

  get selectedSkills(): Skill[] {
    const set = new Set(this.valueIds);
    return this.skills.filter(s => set.has(s.id));
  }

  loadSkills(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.skillService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: skills => {
        this.skills = skills;
        this.applyFilter();
      },
      error: () => {
        this.errorMessage = 'بارگذاری مهارت‌ها انجام نشد.';
        this.isLoading = false;
      },
      complete: () => {
        this.isLoading = false;
      }
    });
  }

  onSearch(event: Event): void {
    this.searchTerm = (event.target as HTMLInputElement).value.trim().toLowerCase();
    this.applyFilter();
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.applyFilter();
  }

  isSelected(id: string): boolean {
    return this.valueIds.includes(id);
  }

  toggleSkill(id: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const next = checked
      ? (this.valueIds.includes(id) ? this.valueIds : [...this.valueIds, id])
      : this.valueIds.filter(x => x !== id);
    this.valueIdsChange.emit(next);
  }

  removeSkill(id: string): void {
    this.valueIdsChange.emit(this.valueIds.filter(x => x !== id));
  }

  clearAll(): void {
    this.valueIdsChange.emit([]);
  }

  private applyFilter(): void {
    if (!this.searchTerm) {
      this.visibleSkills = [...this.skills];
      return;
    }
    this.visibleSkills = this.skills.filter(s =>
      s.name.toLowerCase().includes(this.searchTerm)
    );
  }
}
