
export interface Address {
  postalCode: string;
  country: string;
  state: string;
  city: string;
  streetAddress: string;
  latitude: number;
  longitude: number;
  formattedAddress: string;
}
export interface User {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  address: Address|null;
  timeZoneId: string;
  bio: string|null;
  dateOfBirth: string | null;
  email: string;
  phoneNumber: string;
  skypeId: string;
  hangoutId: string;
  profileVerified: string[]; // An array of verification methods like Email, Mobile
  lessonsCompleted: number | null; // Number of lessons completed
  evaluations: number | null; // The number of evaluations
  // Backend returns the image URL as `imageUrl`. Map it to `profileImagePath` for existing components.
  profileImagePath?: string; // Path or URL to the profile image
  imageUrl?: string;          // Raw field from backend
  profileImage?: File;
  recommendationToken: string;
  isStripeConnected: boolean;
  isPayPalConnected: boolean;
  profileCompletion: number;
}
export interface CompleteProfile {
  firstName: string;
  lastName: string;
  dob: string;
  email: string;
  phone: string;
}
