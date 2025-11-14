import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
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
 * ═══════════════════════════════════════════════════════════════════════
 * AUTH SERVICE - FIXED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * FIXES:
 * ✅ Added platform checks for all window.location usage
 * ✅ Uses DOCUMENT token for SSR safety
 * ✅ Router fallback for SSR (no external redirects on server)
 * ✅ SSR-safe with proper guards
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);
  private readonly bffUrl = environment.bffBaseUrl;

  // ═══════════════════════════════════════════════════════════════
  // STATE MANAGEMENT
  // ═══════════════════════════════════════════════════════════════
  
  private readonly authState = signal<AuthState>({
    isAuthenticated: false,
    user: null,
    error: null,
  });

  private initialized = false;
  private initPromise?: Promise<void>;
  private redirectInProgress = false;
  private redirectTimer?: ReturnType<typeof setTimeout>;
  private refreshSubscription?: Subscription;

  // ═══════════════════════════════════════════════════════════════
  // PUBLIC SELECTORS
  // ═══════════════════════════════════════════════════════════════
  
  readonly isAuthenticated = computed(() => this.authState().isAuthenticated);
  readonly userId = computed(() => this.authState().user?.userId ?? null);
  readonly sessionId = computed(() => this.authState().user?.sessionId ?? null);
  readonly currentUser = computed(() => this.authState().user);
  readonly roles = computed(() => this.authState().user?.roles ?? []);
  readonly authError = computed(() => this.authState().error);
  readonly ready = computed(() => this.initialized);
  readonly activeProfile = computed(() => this.authState().user?.activeProfile);
  readonly hasAdminAccess = computed(() => this.authState().user?.hasAdminAccess);
  readonly tutorProfile = computed(() => this.authState().user?.tutorProfile);
  readonly studentProfile = computed(() => this.authState().user?.studentProfile);

  // ═══════════════════════════════════════════════════════════════
  // INITIALIZATION
  // ═══════════════════════════════════════════════════════════════
  
  async init(): Promise<void> {
    if (this.initialized) return;
    if (this.initPromise) return this.initPromise;
    
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

  // ═══════════════════════════════════════════════════════════════
  // LOGIN / LOGOUT - FIXED
  // ═══════════════════════════════════════════════════════════════
  
  startLogin(returnUrl: string = this.router.url): void {
    // SSR: Use router navigation instead
    if (!this.isBrowser) {
      console.warn('[Auth] Cannot redirect to external login in SSR');
      return;
    }

    const sanitized = this.sanitizeReturnUrl(returnUrl);
    const origin = this.document.location?.origin || '';
    const fullReturnUrl = `${origin}${sanitized}`;
    
    console.info('[Auth] Starting login flow', { returnUrl: sanitized });
    
    this.document.location!.href = `${this.bffUrl}/auth/login?returnUrl=${encodeURIComponent(fullReturnUrl)}`;
  }

  logout(): void {
    // SSR: Use router navigation instead
    if (!this.isBrowser) {
      console.warn('[Auth] Cannot redirect to external logout in SSR');
      this.clearState();
      return;
    }

    console.info('[Auth] Logging out');
    this.cleanup();
    
    this.document.location!.assign(`${this.bffUrl}/auth/logout`);
  }

  // ═══════════════════════════════════════════════════════════════
  // 401 HANDLING - FIXED
  // ═══════════════════════════════════════════════════════════════
  
  handleUnauthorized(returnUrl: string = this.router.url): void {
    // SSR: Just clear state, no redirect possible
    if (!this.isBrowser) {
      console.warn('[Auth] 401 in SSR - clearing state');
      this.clearState();
      return;
    }

    if (this.redirectInProgress) {
      console.warn('[Auth] Redirect already in progress, ignoring duplicate 401');
      return;
    }

    this.redirectInProgress = true;
    
    if (this.redirectTimer) {
      clearTimeout(this.redirectTimer);
    }
    
    this.redirectTimer = setTimeout(() => {
      console.warn('[Auth] Redirect timer expired, resetting flag');
      this.redirectInProgress = false;
    }, 5000);

    this.clearState();

    const sanitized = this.sanitizeReturnUrl(returnUrl);
    const origin = this.document.location?.origin || '';
    const fullReturnUrl = `${origin}${sanitized}`;

    console.info('[Auth] 401 detected - redirecting to login', { returnUrl: sanitized });

    this.document.location!.href = `${this.bffUrl}/auth/login?returnUrl=${encodeURIComponent(fullReturnUrl)}`;
  }

  // ═══════════════════════════════════════════════════════════════
  // PROFILE SWITCHING
  // ═══════════════════════════════════════════════════════════════
  
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
          this.refreshUser();
        }),
        catchError((err) => {
          console.error('[Auth] Profile switch failed:', err);
          return of(null);
        })
      );
  }

  refreshUser(): void {
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
          return of(null);
        }),
        finalize(() => {
          this.refreshSubscription = undefined;
        })
      )
      .subscribe();
  }

  // ═══════════════════════════════════════════════════════════════
  // STATE MANAGEMENT
  // ═══════════════════════════════════════════════════════════════
  
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
    this.refreshSubscription?.unsubscribe();
    this.refreshSubscription = undefined;
    
    if (this.redirectTimer) {
      clearTimeout(this.redirectTimer);
      this.redirectTimer = undefined;
    }
    
    this.clearState();
  }

  // ═══════════════════════════════════════════════════════════════
  // URL VALIDATION - FIXED
  // ═══════════════════════════════════════════════════════════════
  
  private sanitizeReturnUrl(url?: string): string {
    if (!url || url === '/') {
      return '/';
    }

    try {
      if (url.startsWith('/') && !url.startsWith('//')) {
        return url;
      }

      // SSR: Can't validate against window.location
      if (!this.isBrowser) {
        return url.startsWith('/') ? url : '/';
      }

      const origin = this.document.location?.origin || '';
      const parsed = new URL(url, origin);
      
      if (parsed.origin === origin) {
        return `${parsed.pathname}${parsed.search}${parsed.hash}`;
      }

      console.warn('[Auth] Rejected invalid return URL:', url);
      return '/';
    } catch {
      console.warn('[Auth] Failed to parse return URL:', url);
      return '/';
    }
  }

  ngOnDestroy(): void {
    this.cleanup();
  }
}
