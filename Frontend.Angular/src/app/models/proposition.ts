import { TransactionPaymentMethod } from "./enums/transaction-payment-method";

export interface Proposition {
    id?: string; // Optional for new lesson proposals
    paymentMethod: TransactionPaymentMethod
    payPalPaymentId: number|null;
    date: Date;
    duration: number; // "HH:mm:ss" format for TimeSpan
    price: number; // e.g., 30.00
    listingId: string;
    studentId: string | null;
  }
