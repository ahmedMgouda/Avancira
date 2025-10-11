export interface TokenResponse {
  access_token: string;
  refresh_token?: string;
  token_type: string;
  expires_in: number;
  scope: string;
}

export interface UserInfoResponse {
  sub: string;
  email?: string;
  email_verified?: boolean;
  given_name?: string;
  family_name?: string;
  name?: string;
  picture?: string;
  role?: string[];
}

export interface PermissionsResponse {
  permissions: string[];
}

export interface UserProfileResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  imageUrl?: string;
}

export interface AuthState {
  isAuthenticated: boolean;
  user: UserProfile | null;
  roles: string[];
  permissions: string[];
  isLoading: boolean;
  error: AuthError | null;
  tokenExpiresAt: number | null;
  refreshInProgress: boolean;
}

export enum AuthErrorType {
  INITIALIZATION_FAILED = 'INITIALIZATION_FAILED',
  TOKEN_EXPIRED = 'TOKEN_EXPIRED',
  REFRESH_FAILED = 'REFRESH_FAILED',
  LOGIN_FAILED = 'LOGIN_FAILED',
  LOGOUT_FAILED = 'LOGOUT_FAILED',
  NETWORK_ERROR = 'NETWORK_ERROR',
  UNAUTHORIZED = 'UNAUTHORIZED',
  PERMISSIONS_LOAD_FAILED = 'PERMISSIONS_LOAD_FAILED',
  INVALID_STATE = 'INVALID_STATE',
}

export interface AuthError {
  type: AuthErrorType;
  message: string;
  timestamp: number;
  originalError?: any;
  retryable: boolean;
}

export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  imageUrl: string;
  roles: string[];
}
