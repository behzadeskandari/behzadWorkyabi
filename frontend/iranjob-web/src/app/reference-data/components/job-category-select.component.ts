import { CommonModule } from '@angular/common';
import { Component, DestroyRef, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { JobCategory } from '../models/reference-data.models';
import { JobCategoryService } from '../services/job-category.service';

@Component({
  selector: 'app-job-category-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './job-category-select.component.html'
})
export class JobCategorySelectComponent implements OnInit {
  @Input() value: string | null = null;
  @Input() label = 'Job category';
  @Output() readonly valueChange = new EventEmitter<string | null>();

  categories: JobCategory[] = [];
  isLoading = false;
  errorMessage = '';

  private readonly destroyRef = inject(DestroyRef);
  private readonly categoryService = inject(JobCategoryService);

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.categoryService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: categories => this.categories = categories,
      error: () => {
        this.errorMessage = 'Unable to load job categories.';
        this.isLoading = false;
      },
      complete: () => this.isLoading = false
    });
  }

  onSelectionChange(event: Event): void {
    const selectedId = (event.target as HTMLSelectElement).value;
    this.valueChange.emit(selectedId || null);
  }
}
