import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth/services/auth.service';
import { JalaliDateService } from '../core/services/jalali-date.service';
import { NotificationItem } from './notification.models';
import { NotificationsService } from './notifications.service';

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  templateUrl: './notifications-page.component.html',
  styleUrl: './notifications-page.component.scss'
})
export class NotificationsPageComponent implements OnInit {
  readonly notifications = inject(NotificationsService);
  readonly dates = inject(JalaliDateService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly items = signal<NotificationItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly total = signal(0);
  readonly pageSize = 20;
  private page = 1;

  ngOnInit(): void { this.load(true); }

  load(reset = false): void {
    if (reset) { this.page = 1; this.items.set([]); }
    this.loading.set(true);
    this.error.set(false);
    this.notifications.list(this.page, this.pageSize).subscribe({
      next: response => {
        this.items.update(current => [...current, ...response.items]);
        this.total.set(response.totalCount);
        this.notifications.unreadCount.set(response.unreadCount);
        this.loading.set(false);
      },
      error: () => { this.error.set(true); this.loading.set(false); }
    });
  }

  markRead(item: NotificationItem): void {
    if (item.isRead) return;
    this.notifications.markRead(item.id).subscribe({
      next: () => {
        this.items.update(items => items.map(current => current.id === item.id
          ? { ...current, isRead: true, readAt: new Date().toISOString() } : current));
        this.notifications.refreshUnreadCount().subscribe({ error: () => undefined });
      },
      error: () => this.error.set(true)
    });
  }

  markAllRead(): void {
    this.notifications.markAllRead().subscribe({
      next: () => {
        const now = new Date().toISOString();
        this.items.update(items => items.map(item => ({ ...item, isRead: true, readAt: item.readAt ?? now })));
        this.notifications.unreadCount.set(0);
      },
      error: () => this.error.set(true)
    });
  }

  open(item: NotificationItem): void {
    this.markRead(item);
    if (item.type === 6) {
      void this.router.navigate(['/messages']);
    } else if (item.type === 7 && item.relatedResourceId) {
      void this.router.navigate(['/jobs', item.relatedResourceId]);
    } else if (item.relatedResourceId && [3, 4, 5].includes(item.type) && this.auth.hasRole('Candidate')) {
      void this.router.navigate(['/candidate/applications', item.relatedResourceId]);
    } else if (item.relatedResourceId && [1, 2].includes(item.type)
      && (this.auth.hasRole('Employer') || this.auth.hasRole('Recruiter'))) {
      void this.router.navigate(['/employer/applications', item.relatedResourceId]);
    }
  }

  loadMore(): void { this.page++; this.load(); }
  get hasMore(): boolean { return this.items().length < this.total(); }
}
