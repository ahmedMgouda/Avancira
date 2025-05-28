export interface Review {
    revieweeId: string;
    date: Date;
    name: string;
    subject: string;
    feedback?: string; // For received or sent reviews
    avatar?: string | null; // Optional avatar URL,
    rating: number | null;
  }
  