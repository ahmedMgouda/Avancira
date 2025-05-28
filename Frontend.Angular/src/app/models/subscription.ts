import { SubscriptionBillingFrequency } from "./enums/subscription-billing-frequency";
import { SubscriptionStatus } from "./enums/subscription-status";
import { User } from "./user";


export interface Subscription {
  id: number;
  userId: string;
  user?: User | null;
  startDate: Date;
  nextBillingDate: Date;
  cancellationDate?: Date | null;
  amount: number; // Decimal is handled as number in TS
  billingFrequency: SubscriptionBillingFrequency;
  status: SubscriptionStatus; // Computed field
}
