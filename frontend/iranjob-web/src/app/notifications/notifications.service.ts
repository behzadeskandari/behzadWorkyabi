import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { NotificationListResponse } from './notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/api/v1/notifications`;
  readonly unreadCount = signal(0);

  list(page = 1, pageSize = 20) {
    return this.http.get<NotificationListResponse>(this.url, { params: { page, pageSize } });
  }

  refreshUnreadCount() {
    return this.http.get<number>(`${this.url}/unread-count`).pipe(
      tap(count => this.unreadCount.set(count))
    );
  }

  markRead(id: string) { return this.http.patch<boolean>(`${this.url}/${id}/read`, {}); }
  markAllRead() { return this.http.patch<number>(`${this.url}/mark-all-read`, {}); }
}
