export interface RegisterUserRequest {
  firstName: string;
  lastName: string;
  userName: string;
  email: string;
  password: string;
  confirmPassword: string;
  phoneNumber?: string;
  timeZoneId?: string;
  referralToken?: string;
  acceptTerms: boolean;
}
