import { UserCardType } from "./enums/user-card-type";

export interface Card {
    id: number;
    last4: string;
    expMonth: number;
    expYear: number;

    type: string; // E.g., 'PayPal', 'Card'
    icon: string; // Path to the icon
    cardType?: string; // Optional, e.g., 'visa', 'mastercard'
    isDefault: boolean; // Indicates if this is the default payment method,
    purpose: UserCardType; // 'Receiving' or 'Paying'
  }
  