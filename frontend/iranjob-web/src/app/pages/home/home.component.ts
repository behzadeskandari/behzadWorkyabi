import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, tap } from 'rxjs/operators';
import { PublicJobSearchService } from '../../jobs/services/public-job-search.service';
import { PublicJobSearchItem, PublicJobSearchPage, PublicJobSearchQuery, PublicJobSort } from '../../jobs/models/public-job.models';
import { JalaliDateService } from '../../core/services/jalali-date.service';
import { AuthService } from '../../auth/services/auth.service';

const EMPTY_PAGE: PublicJobSearchPage = { items: [], page: 1, pageSize: 12, totalCount: 0, totalPages: 0 };

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  private readonly searchService = inject(PublicJobSearchService);
  private readonly jalaliDate = inject(JalaliDateService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly title = 'ایران‌جاب';
  readonly welcomeMessage = 'به پلتفرم استخدامی ایران‌جاب خوش آمدید.';

  results = EMPTY_PAGE;
  isLoading = true;
  hasError = false;
  readonly pageSize = 12;
  readonly currentSort: PublicJobSort = 'Newest';

  ngOnInit(): void {
    this.loadPublishedJobs();
  }

  loadPublishedJobs(): void {
    this.isLoading = true;
    this.hasError = false;

    const query: PublicJobSearchQuery = {
      page: 1,
      pageSize: this.pageSize,
      sort: this.currentSort
    };

    this.searchService.search(query)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        tap(results => {
          this.results = results;
          this.hasError = false;
        }),
        finalize(() => {
          this.isLoading = false;
        })
      )
      .subscribe({
        error: () => {
          this.hasError = true;
          this.isLoading = false;
        }
      });
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.results.totalPages) return;
    this.loadPage(page);
  }

  nextPage(): void {
    if (this.results.page < this.results.totalPages) {
      this.loadPage(this.results.page + 1);
    }
  }

  previousPage(): void {
    if (this.results.page > 1) {
      this.loadPage(this.results.page - 1);
    }
  }

  private loadPage(page: number): void {
    this.isLoading = true;
    this.hasError = false;

    const query: PublicJobSearchQuery = {
      page,
      pageSize: this.pageSize,
      sort: this.currentSort
    };

    this.searchService.search(query)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => { this.isLoading = false; })
      )
      .subscribe({
        next: results => {
          this.results = results;
          this.hasError = false;
        },
        error: () => {
          this.hasError = true;
        }
      });
  }

  formatDate(value: string): string {
    return this.jalaliDate.formatDisplay(value);
  }

  formatCount(value: number): string {
    return this.jalaliDate.toPersianDigits(value.toString());
  }

  retrySearch(): void {
    this.loadPublishedJobs();
  }

  get isCandidate(): boolean {
    return this.authService.hasRole('Candidate');
  }

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }
}

