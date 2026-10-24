import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { JalaliDateService } from '../../../core/services/jalali-date.service';
import { SettingsService } from './settings.service';
import {
  SettingsResponse,
  UpdatePaymentFeeRequest,
  UpdateEmailNotificationsRequest,
  UpdateAppInfoRequest,
} from './settings.models';

type EditSection = 'paymentFee' | 'emailNotifications' | 'appInfo';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss'],
})
export class SettingsComponent implements OnInit {
  private readonly settingsService = inject(SettingsService);
  private readonly jalaliDate = inject(JalaliDateService);

  settings = signal<SettingsResponse | null>(null);
  isLoading = signal<boolean>(true);
  error = signal<string | null>(null);
  saveStatus = signal<{ section: string | null; status: 'idle' | 'saving' | 'success' | 'error'; message: string | null }>({
    section: null,
    status: 'idle',
    message: null,
  });
  loadSettings(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.settingsService.getAllSettings().subscribe({
      next: (settings) => {
        this.settings.set(settings);
        const fee = settings.paymentFee.value as any;
        this.workingPaymentFee = {
          amount: typeof fee?.amount === 'number' ? fee.amount : 0,
          currency: typeof fee?.currency === 'string' ? fee.currency : 'IRR',
        };
        const email = settings.emailNotifications.value as any;
        this.workingEmailNotifications = {
          enabled: typeof email?.enabled === 'boolean' ? email.enabled : true,
          fromAddress: typeof email?.fromAddress === 'string' ? email.fromAddress : '',
        };
        const app = settings.appInfo.value as any;
        this.workingAppInfo = {
          name: typeof app?.name === 'string' ? app.name : '',
          version: typeof app?.version === 'string' ? app.version : '',
        };
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('خطا در بارگذاری تنظیمات سامانه. لطفاً دوباره تلاش کنید.');
        this.isLoading.set(false);
      },
    });
  }

  toggleEdit(section: EditSection): void {
    this.editState.update((s) => ({ ...s, [section]: !s[section] }));
    if (!this.editState()[section]) {
      this.resetWorkingValues(section);
    }
  }

  editState = signal<{
    paymentFee: boolean;
    emailNotifications: boolean;
    appInfo: boolean;
  }>({ paymentFee: false, emailNotifications: false, appInfo: false });

  workingPaymentFee = { amount: 0, currency: 'IRR' };
  workingEmailNotifications = { enabled: true, fromAddress: '' };
  workingAppInfo = { name: '', version: '' };

  ngOnInit(): void {
    this.loadSettings();
  }

  private resetWorkingValues(section: EditSection): void {
    const s = this.settings();
    if (!s) return;
    if (section === 'paymentFee') {
      const fee = s.paymentFee.value as any;
      this.workingPaymentFee = {
        amount: typeof fee?.amount === 'number' ? fee.amount : 0,
        currency: typeof fee?.currency === 'string' ? fee.currency : 'IRR',
      };
    } else if (section === 'emailNotifications') {
      const email = s.emailNotifications.value as any;
      this.workingEmailNotifications = {
        enabled: typeof email?.enabled === 'boolean' ? email.enabled : true,
        fromAddress: typeof email?.fromAddress === 'string' ? email.fromAddress : '',
      };
    } else if (section === 'appInfo') {
      const app = s.appInfo.value as any;
      this.workingAppInfo = {
        name: typeof app?.name === 'string' ? app.name : '',
        version: typeof app?.version === 'string' ? app.version : '',
      };
    }
  }

  savePaymentFee(): void {
    const request: UpdatePaymentFeeRequest = {
      amount: this.workingPaymentFee.amount,
      currency: this.workingPaymentFee.currency,
    };
    this.saveStatus.set({ section: 'paymentFee', status: 'saving', message: null });
    this.settingsService.updatePaymentFee(request).subscribe({
      next: (response) => {
        this.saveStatus.set({ section: 'paymentFee', status: 'success', message: 'مبلغ پرداخت با موفقیت بروزرسانی شد.' });
        this.editState.update((s) => ({ ...s, paymentFee: false }));
        this.loadSettings();
      },
      error: (err) => {
        const msg = err.error?.message || 'خطا در بروزرسانی مبلغ پرداخت.';
        this.saveStatus.set({ section: 'paymentFee', status: 'error', message: msg });
      },
    });
  }

  saveEmailNotifications(): void {
    const request: UpdateEmailNotificationsRequest = {
      enabled: this.workingEmailNotifications.enabled,
      fromAddress: this.workingEmailNotifications.fromAddress,
    };
    this.saveStatus.set({ section: 'emailNotifications', status: 'saving', message: null });
    this.settingsService.updateEmailNotifications(request).subscribe({
      next: (response) => {
        this.saveStatus.set({ section: 'emailNotifications', status: 'success', message: 'تنظیمات ایمیل با موفقیت بروزرسانی شد.' });
        this.editState.update((s) => ({ ...s, emailNotifications: false }));
        this.loadSettings();
      },
      error: (err) => {
        const msg = err.error?.message || 'خطا در بروزرسانی تنظیمات ایمیل.';
        this.saveStatus.set({ section: 'emailNotifications', status: 'error', message: msg });
      },
    });
  }

  saveAppInfo(): void {
    const request: UpdateAppInfoRequest = {
      name: this.workingAppInfo.name,
      version: this.workingAppInfo.version,
    };
    this.saveStatus.set({ section: 'appInfo', status: 'saving', message: null });
    this.settingsService.updateAppInfo(request).subscribe({
      next: (response) => {
        this.saveStatus.set({ section: 'appInfo', status: 'success', message: 'اطلاعات برنامه با موفقیت بروزرسانی شد.' });
        this.editState.update((s) => ({ ...s, appInfo: false }));
        this.loadSettings();
      },
      error: (err) => {
        const msg = err.error?.message || 'خطا در بروزرسانی اطلاعات برنامه.';
        this.saveStatus.set({ section: 'appInfo', status: 'error', message: msg });
      },
    });
  }

  cancelEdit(section: EditSection): void {
    this.resetWorkingValues(section);
    this.editState.update((s) => ({ ...s, [section]: false }));
    this.saveStatus.set({ section: null, status: 'idle', message: null });
  }

  getSectionValue(section: { key: string; label: string; description: string; source: string; value: unknown }): string {
    const val = section.value;
    if (val === null || val === undefined) return '-';
    if (typeof val === 'object') {
      return Object.entries(val as Record<string, unknown>)
        .map(([k, v]) => `${k}: ${v}`)
        .join(', ');
    }
    return String(val);
  }

  isReadOnly(section: { source: string }): boolean {
    return section.source === 'deployment_config';
  }

  getPaymentFeeCurrency(): string {
    const s = this.settings();
    if (!s) return 'IRR';
    const val = s.paymentFee.value as any;
    return typeof val?.currency === 'string' ? val.currency : 'IRR';
  }

  getEmailNotificationsEnabled(): string {
    const s = this.settings();
    if (!s) return 'خیر';
    const val = s.emailNotifications.value as any;
    return typeof val?.enabled === 'boolean' && val.enabled ? 'بله' : 'خیر';
  }

  getEmailFromAddress(): string {
    const s = this.settings();
    if (!s) return '-';
    const val = s.emailNotifications.value as any;
    return typeof val?.fromAddress === 'string' ? val.fromAddress : '-';
  }

  getAppName(): string {
    const s = this.settings();
    if (!s) return '-';
    const val = s.appInfo.value as any;
    return typeof val?.name === 'string' ? val.name : '-';
  }

  getAppVersion(): string {
    const s = this.settings();
    if (!s) return '-';
    const val = s.appInfo.value as any;
    return typeof val?.version === 'string' ? val.version : '-';
  }

  clearSaveStatus(): void {
    this.saveStatus.set({ section: null, status: 'idle', message: null });
  }
}