import { SubscriptionBillingFrequency } from './enums/subscription-billing-frequency';
import { TransactionPaymentMethod } from './enums/transaction-payment-method';
import { TransactionPaymentType } from './enums/transaction-payment-type';

export interface SubscriptionRequest {
  payPalPaymentId?: string | null;
  promoCode?: string | null;
  amount?: number | null;
  paymentMethod: TransactionPaymentMethod;
  description?: string;
  paymentType: TransactionPaymentType;
  billingFrequency: SubscriptionBillingFrequency;
}
