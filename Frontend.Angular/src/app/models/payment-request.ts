export interface PaymentRequest {
  gateway: string;
  amount: number;
  currency: string;
  returnUrl: string;
  cancelUrl: string;
  listingId?: number | null;
}
