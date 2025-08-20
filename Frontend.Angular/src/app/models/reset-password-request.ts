export interface ResetPasswordRequest {
  userId: string;
  password: string;
  confirmPassword: string;
  token: string;
}
