export interface NotificationItem {
  id: string;
  type: number;
  title: string;
  message: string;
  relatedResourceId: string | null;
  createdAt: string;
  isRead: boolean;
  readAt: string | null;
}

export interface NotificationListResponse {
  items: NotificationItem[];
  totalCount: number;
  unreadCount: number;
  page: number;
  pageSize: number;
}
