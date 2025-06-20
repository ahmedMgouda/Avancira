import { SubscriptionBillingFrequency } from './enums/subscription-billing-frequency';
import { SubscriptionHistory } from './subscription-history';

export interface SubscriptionDetails {
  plan: string;
  billingFrequency: SubscriptionBillingFrequency;
  status: string;
  startDate: Date;
  nextBillingDate: Date;
  nextBillingAmount: number;
  paymentMethod: string;
  subscriptionHistory: SubscriptionHistory[];
}
