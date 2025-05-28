import { Transaction } from "./transaction";

export interface PaymentHistory {
  walletBalance: number;
  totalAmountCollected: number;
  invoices: Transaction[];
  transactions: Transaction[];
}
