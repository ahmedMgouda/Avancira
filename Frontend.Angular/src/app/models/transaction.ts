export interface Transaction {
    id: number;
    senderId: number;
    senderName: string;
    recipientId?: number; // Nullable
    recipientName: string;
    amount: number;
    platformFee: number;
    net: number;
    status: string;
    transactionType: string;
    description: string;
    transactionDate: Date;
    date: Date;
    type: string;
  }
  