import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { JalaliDateService } from '../../../core/services/jalali-date.service';
import { PaymentsService } from './payments.service';
import {
  PaginatedPaymentsResponse,
  PaymentDetail,
  PaymentFilter,
  PAYMENT_STATUS_LABELS,
  PAYMENT_METHOD_LABELS,
} from './payments.models';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payments.component.html',
  styleUrls: ['./payments.component.scss'],
})
export class PaymentsComponent implements OnInit, OnDestroy {
  private readonly paymentsService = inject(PaymentsService);
  private readonly jalaliDate = inject(JalaliDateService);

  // ── Data state ──
  paymentList = signal<any[]>([]);
  paymentSummary = signal<any | null>(null);
  selectedPayment = signal<any | null>(null);
  isLoading = signal<boolean>(true);
  error = signal<string | null>(null);

  // ── Pagination ──
  currentPage = signal<number>(1);
  pageSize = signal<number>(20);
  totalCount = signal<number>(0);
  totalPages = signal<number>(1);

  // ── Filter state ──
  filter = signal<PaymentFilter>({});
  readonly pageSizeOptions = [10, 20, 50, 100];

  // ── Status and method label maps ──
  readonly statusLabels = PAYMENT_STATUS_LABELS;
  readonly methodLabels = PAYMENT_METHOD_LABELS;

  // ── Search debounce ──
  private readonly searchSubject = new Subject<string>();
  private readonly searchDebounceMs = 500;

  // ── View mode ──
  viewMode: 'list' | 'detail' = 'list';

  ngOnInit(): void {
    this.loadData();
    this.setupSearchDebounce();
  }

  ngOnDestroy(): void {
    this.searchSubject.complete();
  }

  private setupSearchDebounce(): void {
    this.searchSubject
      .pipe(debounceTime(this.searchDebounceMs), distinctUntilChanged())
      .subscribe(() => {
        this.currentPage.set(1);
        this.loadPayments();
      });
  }

  public loadData(): void {
    this.loadPayments();
    this.loadSummary();
  }

  loadPayments(): void {
    this.isLoading.set(true);
    this.error.set(null);

    const f = this.filter();
    this.paymentsService
      .getAllPayments(this.currentPage(), this.pageSize(), f)
      .subscribe({
        next: (response) => {
          this.paymentList.set(response.items);
          this.totalCount.set(response.totalCount);
          this.totalPages.set(Math.ceil(response.totalCount / this.pageSize()));
          this.isLoading.set(false);
        },
        error: () => {
          this.error.set(
            'خطا در بارگذاری لیست پرداخت‌ها. لطفاً دوباره تلاش کنید.'
          );
          this.isLoading.set(false);
        },
      });
  }

  loadSummary(): void {
    this.paymentsService.getPaymentSummary().subscribe({
      next: (summary) => this.paymentSummary.set(summary),
      error: () => {
        // Summary failure is non-fatal; continue with list only.
      },
    });
  }

  onSearchInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchSubject.next(target.value);
  }

  onFilterChange(): void {
    this.currentPage.set(1);
    this.loadPayments();
  }

  onPageChange(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.loadPayments();
  }

  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.currentPage.set(1);
    this.loadPayments();
  }

  openDetail(paymentId: string): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.paymentsService.getPaymentDetail(paymentId).subscribe({
      next: (detail) => {
        this.selectedPayment.set(detail);
        this.viewMode = 'detail';
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('خطا در بارگذاری جزئیات پرداخت.');
        this.isLoading.set(false);
      },
    });
  }

  backToList(): void {
    this.viewMode = 'list';
    this.selectedPayment.set(null);
  }

  formatDate(dateString: string | null): string {
    if (!dateString) return '-';
    try {
      return this.jalaliDate.formatDisplay(dateString);
    } catch {
      return '-';
    }
  }

  formatDateTime(dateString: string | null): string {
    if (!dateString) return '-';
    try {
      return this.jalaliDate.formatDisplayDateTime(dateString);
    } catch {
      return '-';
    }
  }

  formatAmount(value: number): string {
    return this.jalaliDate.toPersianDigits(value.toString());
  }

  getStatusBadgeClass(status: string): string {
    const base = 'badge';
    switch (status) {
      case 'Paid':
        return `${base} badge--success`;
      case 'Pending':
        return `${base} badge--warning`;
      case 'Failed':
      case 'Expired':
        return `${base} badge--danger`;
      default:
        return base;
    }
  }

  getPaginationRange(): string {
    const start = (this.currentPage() - 1) * this.pageSize() + 1;
    const end = Math.min(this.currentPage() * this.pageSize(), this.totalCount());
    if (this.totalCount() === 0) return '0';
    return `${start} تا ${end} از ${this.formatAmount(this.totalCount())}`;
  }

  resetFilters(): void {
    this.filter.set({});
    this.currentPage.set(1);
    this.loadPayments();
  }

  hasPayments(): boolean {
    return this.paymentList().length > 0;
  }
}
