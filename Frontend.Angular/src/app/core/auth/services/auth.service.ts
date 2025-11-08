import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { 
  catchError, 
  finalize, 
  firstValueFrom, 
  map, 
  of, 
  Subscription, 
  tap, 
  timeout 
} from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthState, UserProfile } from '../models/auth.models';

/**
 * AuthService - BFF Pattern
 * 
 * Responsibilities:
 * 1. Check authentication status with BFF
 * 2. Manage user state (signals)
 * 3. Handle login/logout redirects
 * 4. Profile switching (student/tutor/admin)
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly bffUrl = environment.bffBaseUrl;

  // ═══════════════════════════════════════════════════════════
  // STATE MANAGEMENT (Signals)
  // ═══════════════════════════════════════════════════════════
  
  private readonly authState = signal<AuthState>({
    isAuthenticated: false,
    user: null,
    error: null,
  });

  private initialized = false;
  private initPromise?: Promise<void>;

  // Guard against redirect storms
  private redirectInProgress = false;
  private redirectTimer?: ReturnType<typeof setTimeout>;

  // Track subscriptions for cleanup
  private refreshSubscription?: Subscription;

  // ═══════════════════════════════════════════════════════════
  // PUBLIC COMPUTED SELECTORS
  // ═══════════════════════════════════════════════════════════
  
  readonly isAuthenticated = computed(() => this.authState().isAuthenticated);
  readonly userId = computed(() => this.authState().user?.userId ?? null);
  readonly sessionId = computed(() => this.authState().user?.sessionId ?? null);

  readonly currentUser = computed(() => this.authState().user);
  readonly roles = computed(() => this.authState().user?.roles ?? []);
  readonly authError = computed(() => this.authState().error);
  readonly ready = computed(() => this.initialized);

  // Profile-specific selectors
  readonly activeProfile = computed(() => this.authState().user?.activeProfile);
  readonly hasAdminAccess = computed(() => this.authState().user?.hasAdminAccess);
  readonly tutorProfile = computed(() => this.authState().user?.tutorProfile);
  readonly studentProfile = computed(() => this.authState().user?.studentProfile);

  // ═══════════════════════════════════════════════════════════
  // INITIALIZATION
  // ═══════════════════════════════════════════════════════════
  
  /**
   * Initialize auth state by checking with BFF
   * Called once during app startup (APP_INITIALIZER)
   */
  async init(): Promise<void> {
    if (this.initialized) {
      return;
    }
    
    if (this.initPromise) {
      return this.initPromise;
    }
    
    this.initPromise = this.performInit();
    return this.initPromise;
  }

  private async performInit(): Promise<void> {
    try {
      console.info('[Auth] Initializing - checking session with BFF...');
      
      const response = await firstValueFrom(
        this.http
          .get<{ isAuthenticated: boolean; error?: string } & UserProfile>(
            `${this.bffUrl}/auth/user`,
            { withCredentials: true }
          )
          .pipe(
            timeout(15000),
            catchError((err) => {
              console.warn('[Auth] Failed to load user session:', err);
              return of({ isAuthenticated: false } as any);
            })
          )
      );

      if (response.isAuthenticated) {
        this.setAuthenticatedUser(response);
        console.info('[Auth] ✓ User authenticated:', response.userId);
      } else {
        this.clearState();
        console.info('[Auth] ✗ User not authenticated');
      }
    } catch (error) {
      console.error('[Auth] Init error:', error);
      this.clearState();
    } finally {
      this.initialized = true;
    }
  }

  // ═══════════════════════════════════════════════════════════
  // LOGIN / LOGOUT
  // ═══════════════════════════════════════════════════════════
  
  /**
   * Redirect to BFF login endpoint
   * BFF will handle OIDC flow and redirect back
   */
  startLogin(returnUrl: string = this.router.url): void {
    const sanitized = this.sanitizeReturnUrl(returnUrl);
    const fullReturnUrl = `${window.location.origin}${sanitized}`;
    
    console.info('[Auth] Starting login flow', { returnUrl: sanitized });
    
    // BFF will handle OIDC flow and return with cookie
    window.location.href = `${this.bffUrl}/auth/login?returnUrl=${encodeURIComponent(fullReturnUrl)}`;
  }

  /**
   * Logout - redirect to BFF logout endpoint
   * BFF will clear session and redirect to OIDC logout
   */
  logout(): void {
    console.info('[Auth] Logging out');
    
    // Clean up local state
    this.cleanup();
    
    // BFF will handle OIDC logout and cookie removal
    window.location.assign(`${this.bffUrl}/auth/logout`);
  }

  // ═══════════════════════════════════════════════════════════
  // 401 HANDLING (Called by AuthInterceptor)
  // ═══════════════════════════════════════════════════════════
  
  /**
   * Handle unauthorized access (401 response from BFF)
   * 
   * Guards against redirect storms:
   * - Only allows one redirect at a time
   * - Resets flag after 5 seconds (in case of failure)
   */
  handleUnauthorized(returnUrl: string = this.router.url): void {
    // Guard against multiple simultaneous 401s
    if (this.redirectInProgress) {
      console.warn('[Auth] Redirect already in progress, ignoring duplicate 401');
      return;
    }

    this.redirectInProgress = true;
    
    // Safety: reset flag after 5s in case redirect fails
    if (this.redirectTimer) {
      clearTimeout(this.redirectTimer);
    }
    
    this.redirectTimer = setTimeout(() => {
      console.warn('[Auth] Redirect timer expired, resetting flag');
      this.redirectInProgress = false;
    }, 5000);

    // Clear local state
    this.clearState();

    const sanitized = this.sanitizeReturnUrl(returnUrl);
    const fullReturnUrl = `${window.location.origin}${sanitized}`;

    console.info('[Auth] 401 detected - redirecting to login', { 
      returnUrl: sanitized 
    });

    // Redirect to login
    window.location.href = `${this.bffUrl}/auth/login?returnUrl=${encodeURIComponent(fullReturnUrl)}`;
  }

  // ═══════════════════════════════════════════════════════════
  // PROFILE SWITCHING (Student/Tutor/Admin)
  // ═══════════════════════════════════════════════════════════
  
  /**
   * Switch active profile
   * BFF updates session, then we refresh user state
   */
  switchProfile(target: 'student' | 'tutor' | 'admin') {
    console.info('[Auth] Switching profile to:', target);
    
    return this.http
      .post<{ activeProfile: string }>(
        `${this.bffUrl}/auth/switch-profile/${target}`,
        {},
        { withCredentials: true }
      )
      .pipe(
        tap((response) => {
          console.info('[Auth] Profile switched:', response.activeProfile);
          // Refresh user data to get updated profile
          this.refreshUser();
        }),
        catchError((err) => {
          console.error('[Auth] Profile switch failed:', err);
          return of(null);
        })
      );
  }

  /**
   * Re-fetch user profile from BFF
   * Used after profile switching or other state changes
   * 
   * FIX: Properly manages subscription to prevent memory leaks
   */
  refreshUser(): void {
    // Cancel any pending refresh
    this.refreshSubscription?.unsubscribe();

    console.debug('[Auth] Refreshing user profile from BFF...');
    
    this.refreshSubscription = this.http
      .get<{ isAuthenticated: boolean } & UserProfile>(
        `${this.bffUrl}/auth/user`,
        { withCredentials: true }
      )
      .pipe(
        timeout(10000),
        map((response) => {
          if (response.isAuthenticated) {
            this.setAuthenticatedUser(response);
            console.info('[Auth] ✓ User profile refreshed');
          } else {
            this.clearState();
            console.warn('[Auth] ✗ User not authenticated after refresh');
          }
          return response;
        }),
        catchError((err) => {
          console.error('[Auth] Refresh failed:', err);
          // Don't clear state on network errors - user might still be logged in
          return of(null);
        }),
        finalize(() => {
          // Clean up subscription reference
          this.refreshSubscription = undefined;
        })
      )
      .subscribe();
  }

  // ═══════════════════════════════════════════════════════════
  // HELPERS - State Management
  // ═══════════════════════════════════════════════════════════
  
  private setAuthenticatedUser(response: UserProfile & { isAuthenticated: boolean }): void {
    const user: UserProfile = {
      userId: response.userId,
      sessionId: response.sessionId,
      firstName: response.firstName,
      lastName: response.lastName,
      fullName: response.fullName,
      profileImageUrl: response.profileImageUrl,
      roles: response.roles,
      activeProfile: response.activeProfile,
      hasAdminAccess: response.hasAdminAccess,
      tutorProfile: response.tutorProfile,
      studentProfile: response.studentProfile,
    };

    this.authState.set({
      isAuthenticated: true,
      user,
      error: null,
    });
  }

  private clearState(): void {
    this.authState.set({
      isAuthenticated: false,
      user: null,
      error: null,
    });
  }

  private cleanup(): void {
    // Cancel any pending operations
    this.refreshSubscription?.unsubscribe();
    this.refreshSubscription = undefined;
    
    // Clear redirect timer
    if (this.redirectTimer) {
      clearTimeout(this.redirectTimer);
      this.redirectTimer = undefined;
    }
    
    // Clear state
    this.clearState();
  }

  // ═══════════════════════════════════════════════════════════
  // HELPERS - URL Validation
  // ═══════════════════════════════════════════════════════════
  
  /**
   * Sanitize return URL to prevent open redirects
   */
  private sanitizeReturnUrl(url?: string): string {
    if (!url || url === '/') {
      return '/';
    }

    try {
      // Allow local paths
      if (url.startsWith('/') && !url.startsWith('//')) {
        return url;
      }

      // Validate absolute URLs
      const parsed = new URL(url, window.location.origin);
      
      // Only allow same origin
      if (parsed.origin === window.location.origin) {
        return `${parsed.pathname}${parsed.search}${parsed.hash}`;
      }

      console.warn('[Auth] Rejected invalid return URL:', url);
      return '/';
    } catch {
      console.warn('[Auth] Failed to parse return URL:', url);
      return '/';
    }
  }

  // ═══════════════════════════════════════════════════════════
  // CLEANUP
  // ═══════════════════════════════════════════════════════════
  
  /**
   * Called when service is destroyed (rare, but good practice)
   */
  ngOnDestroy(): void {
    this.cleanup();
  }
}