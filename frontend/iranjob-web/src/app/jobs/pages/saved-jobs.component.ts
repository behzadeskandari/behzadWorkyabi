import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JalaliDateService } from '../../core/services/jalali-date.service';
import { CandidateJobPreferencesService } from '../services/candidate-job-preferences.service';
import { SavedJobItem } from '../models/candidate-job-preferences.models';

@Component({ selector: 'app-saved-jobs', standalone: true, imports: [RouterLink], templateUrl: './saved-jobs.component.html', styleUrl: './saved-jobs.component.scss' })
export class SavedJobsComponent implements OnInit {
  private readonly prefs = inject(CandidateJobPreferencesService);
  readonly dates = inject(JalaliDateService);
  items: SavedJobItem[] = [];
  page = 1;
  pageSize = 20;
  total = 0;
  loading = true;
  error = false;

  ngOnInit(): void { this.load(); }
  load(): void {
    this.loading = true; this.error = false;
    this.prefs.listSaved(this.page, this.pageSize).subscribe({
      next: result => { this.items = result.items; this.total = result.totalCount; this.loading = false; },
      error: () => { this.error = true; this.loading = false; }
    });
  }
  remove(item: SavedJobItem): void {
    this.prefs.remove(item.jobPostingId).subscribe({ next: () => {
      if (this.items.length === 1 && this.page > 1) this.page--;
      this.load();
    }, error: () => this.error = true });
  }
}
