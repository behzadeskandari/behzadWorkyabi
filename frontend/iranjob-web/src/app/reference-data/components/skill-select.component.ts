import { CommonModule } from '@angular/common';
import { Component, DestroyRef, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Skill } from '../models/reference-data.models';
import { SkillService } from '../services/skill.service';

@Component({
  selector: 'app-skill-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './skill-select.component.html'
})
export class SkillSelectComponent implements OnInit {
  @Input() valueIds: string[] = [];
  @Input() label = 'Skills';
  @Output() readonly valueIdsChange = new EventEmitter<string[]>();

  skills: Skill[] = [];
  isLoading = false;
  errorMessage = '';

  private readonly destroyRef = inject(DestroyRef);
  private readonly skillService = inject(SkillService);

  ngOnInit(): void {
    this.loadSkills();
  }

  loadSkills(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.skillService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: skills => this.skills = skills,
      error: () => {
        this.errorMessage = 'Unable to load skills.';
        this.isLoading = false;
      },
      complete: () => this.isLoading = false
    });
  }

  onSelectionChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.valueIdsChange.emit(Array.from(select.selectedOptions, option => option.value));
  }
}
