/**
 * Strongly typed user roles.
 */
export enum UserRole {
  Admin = 'admin',
  User = 'user',
}

export type UserRoleType = `${UserRole}`;

/**
 * User identity and authorization information.
 * Returned directly from /bff/auth/user.
 */
export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  imageUrl: string;
  roles: UserRoleType[];
  permissions: string[];
}

/**
 * Reactive authentication state for the current session.
 */
export interface AuthState {
  isAuthenticated: boolean;
  user: UserProfile | null;
  error: AuthError | null;
}

/**
 * Lightweight auth error type.
 */
export enum AuthErrorType {
  NETWORK = 'NETWORK',
  SERVER = 'SERVER',
  UNAUTHORIZED = 'UNAUTHORIZED',
}

/**
 * Error structure for authentication issues.
 */
export interface AuthError {
  type: AuthErrorType;
  message: string;
  timestamp: number;
}
