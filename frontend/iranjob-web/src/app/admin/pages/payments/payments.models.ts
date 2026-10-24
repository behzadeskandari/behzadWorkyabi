export interface PaymentListItem {
  id: string;
  amount: number;
  currency: string;
  status: string;
  paymentMethod: string;
  authority: string;
  createdAt: string;
  paidAt: string | null;
  failedAt: string | null;
  failureReason: string | null;
  relatedJobPostingId: string | null;
  employerProfileId: string;
}

export interface PaymentDetail {
  id: string;
  amount: number;
  currency: string;
  status: string;
  paymentMethod: string;
  authority: string;
  description: string;
  callbackUrl: string | null;
  relatedJobPostingId: string | null;
  employerProfileId: string;
  createdAt: string;
  paidAt: string | null;
  failedAt: string | null;
  failureReason: string | null;
  publicationStatus: string;
}

export interface PaymentSummary {
  total: number;
  successful: number;
  pending: number;
  failed: number;
  totalAmount: number;
  successfulAmount: number;
}

export interface PaginatedPaymentsResponse {
  items: PaymentListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface PaymentFilter {
  search?: string;
  status?: string;
  paymentMethod?: string;
  fromDate?: string;
  toDate?: string;
}

export const PAYMENT_STATUS_LABELS: Record<string, string> = {
  Pending: 'در انتظار',
  Paid: 'پرداخت شده',
  Failed: 'ناموفق',
  Expired: 'منقضی شده',
};

export const PAYMENT_METHOD_LABELS: Record<string, string> = {
  ZarinPal: 'زرین‌پال',
};