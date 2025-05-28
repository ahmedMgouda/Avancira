
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
  dateOfBirth: string;
  email: string;
  phoneNumber: string;
  skypeId: string;
  hangoutId: string;
  profileVerified: string[]; // An array of verification methods like Email, Mobile
  lessonsCompleted: string; // Could be a duration string like '77h'
  evaluations: number; // The number of evaluations
  profileImagePath: string; // Path or URL to the profile image
  profileImage: File; // Path or URL to the profile image
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
