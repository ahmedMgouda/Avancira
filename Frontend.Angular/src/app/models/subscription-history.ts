import { SubscriptionBillingFrequency } from './enums/subscription-billing-frequency';

export interface SubscriptionHistory {
  action: string;
  status: string;
  changeDate: Date;
  billingFrequency: SubscriptionBillingFrequency;
  amount: number;
}
