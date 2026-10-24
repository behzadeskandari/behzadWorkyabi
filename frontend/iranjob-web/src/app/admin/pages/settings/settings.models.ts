export interface SettingsSection {
  key: string;
  label: string;
  description: string;
  source: string;
  value: unknown;
}

export interface SettingsResponse {
  paymentFee: SettingsSection;
  emailNotifications: SettingsSection;
  appInfo: SettingsSection;
  all: Record<string, string>;
}

export interface UpdatePaymentFeeRequest {
  amount: number;
  currency: string;
}

export interface PaymentFeeResponse {
  amount: number;
  currency: string;
}

export interface UpdateEmailNotificationsRequest {
  enabled: boolean;
  fromAddress: string;
}

export interface EmailNotificationsResponse {
  enabled: boolean;
  fromAddress: string;
}

export interface UpdateAppInfoRequest {
  name: string;
  version: string;
}

export interface AppInfoResponse {
  name: string;
  version: string;
}

export interface SettingEditState {
  [key: string]: {
    editing: boolean;
    value: unknown;
    originalValue: unknown;
  };
}