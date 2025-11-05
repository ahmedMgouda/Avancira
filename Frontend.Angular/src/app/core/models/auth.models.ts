/**
 * ===========================================================
 * User Roles and Profiles
 * ===========================================================
 */

export enum UserRole {
  Admin = 'admin',
  Tutor = 'tutor',
  Student = 'student',
}

export type UserRoleType = `${UserRole}`;

/**
 * Tutor sub-profile
 */
export interface TutorProfile {
  isActive: boolean;
  isVerified: boolean;
  isComplete: boolean;
  showReminder: boolean;
}

/**
 * Student sub-profile
 */
export interface StudentProfile {
  canBook: boolean;
  subscriptionStatus: string;
  subscriptionEndsOnUtc: string | null;
  isComplete: boolean;
  showReminder: boolean;
}

/**
 * ===========================================================
 * Updated User Profile (matches /bff/auth/user)
 * ===========================================================
 */
export interface UserProfile {
  userId: string;
  sessionId: string;
  firstName: string;
  lastName: string;
  fullName: string;
  profileImageUrl?: string | null;
  roles: UserRoleType[];
  activeProfile: 'tutor' | 'student';
  hasAdminAccess: boolean;
  tutorProfile?: TutorProfile | null;
  studentProfile?: StudentProfile | null;
}

/**
 * ===========================================================
 * Auth State & Errors
 * ===========================================================
 */

export interface AuthState {
  isAuthenticated: boolean;
  user: UserProfile | null;
  error: AuthError | null;
}

export enum AuthErrorType {
  NETWORK = 'NETWORK',
  SERVER = 'SERVER',
  UNAUTHORIZED = 'UNAUTHORIZED',
}

export interface AuthError {
  type: AuthErrorType;
  message: string;
  timestamp: number;
}
