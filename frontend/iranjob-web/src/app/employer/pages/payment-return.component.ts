import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EmployerPayment, EmployerPaymentService } from '../services/employer-payment.service';

@Component({
  selector: 'app-employer-payment-return',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './payment-return.component.html',
  styleUrl: './payment-return.component.scss'
})
export class EmployerPaymentReturnComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly payments = inject(EmployerPaymentService);

  payment: EmployerPayment | null = null;
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.refreshStatus();
  }

  refreshStatus(): void {
    const params = this.route.snapshot.queryParamMap;
    const paymentId = params.get('paymentId');
    const authority = params.get('Authority') ?? params.get('authority');
    const providerStatus = params.get('Status') ?? params.get('status') ?? '';
    if (!paymentId) {
      this.errorMessage = 'اطلاعات بازگشت از درگاه کامل نیست. وضعیت را از فهرست آگهی‌های خود بررسی کنید.';
      this.isLoading = false;
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    if (!authority) {
      this.loadOwnedPayment(paymentId);
      return;
    }
    this.payments.verifyCallback(authority, providerStatus).subscribe({
      next: () => this.loadOwnedPayment(paymentId),
      error: () => {
        this.errorMessage = 'تأیید پرداخت در دسترس نیست. می‌توانید وضعیت را دوباره بررسی کنید.';
        this.isLoading = false;
      }
    });
  }

  private loadOwnedPayment(paymentId: string): void {
    this.payments.getForEmployer(paymentId).subscribe({
      next: payment => {
        this.payment = payment;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'پرداخت برای حساب کاربری شما پیدا نشد یا هنوز قابل بررسی نیست.';
        this.isLoading = false;
      }
    });
  }
}
