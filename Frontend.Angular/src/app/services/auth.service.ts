import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, firstValueFrom, map, of, tap, timeout } from 'rxjs';

import { AuthState, UserProfile } from '../core/models/auth.models';
import { environment } from '../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  /** Base BFF endpoint */
  private readonly bffUrl = environment.bffBaseUrl;

  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  /** Reactive auth state */
  private readonly authState = signal<AuthState>({
    isAuthenticated: false,
    user: null,
    error: null,
  });

  private initialized = false;
  private initPromise?: Promise<void>;

  // ───────────────────────────────────────────────
  // Computed selectors
  // ───────────────────────────────────────────────
  readonly isAuthenticated = computed(() => this.authState().isAuthenticated);
  readonly currentUser = computed(() => this.authState().user);
  readonly roles = computed(() => this.authState().user?.roles ?? []);
  readonly authError = computed(() => this.authState().error);
  readonly ready = computed(() => this.initialized);

  readonly activeProfile = computed(() => this.authState().user?.activeProfile);
  readonly hasAdminAccess = computed(() => this.authState().user?.hasAdminAccess);
  readonly tutorProfile = computed(() => this.authState().user?.tutorProfile);
  readonly studentProfile = computed(() => this.authState().user?.studentProfile);

  // ───────────────────────────────────────────────
  // Initialization
  // ───────────────────────────────────────────────
  async init(): Promise<void> {
    if (this.initialized) return;
    if (this.initPromise) return this.initPromise;
    this.initPromise = this.performInit();
    return this.initPromise;
  }

  private async performInit(): Promise<void> {
    try {
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
        const user: UserProfile = {
          userId: response.userId,
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

        this.setState({ isAuthenticated: true, user, error: null });
      } else {
        this.clearState();
      }
    } catch (error) {
      console.error('[Auth] Init error:', error);
      this.clearState();
    } finally {
      this.initialized = true;
    }
  }

  // ───────────────────────────────────────────────
  // Login / Logout
  // ───────────────────────────────────────────────
  startLogin(returnUrl: string = this.router.url): void {
    const sanitized = this.sanitizeReturnUrl(returnUrl);
    sessionStorage.setItem('auth:return_url', sanitized);

    const fullReturnUrl = `${window.location.origin}${sanitized}`;
    window.location.href = `${this.bffUrl}/auth/login?returnUrl=${encodeURIComponent(fullReturnUrl)}`;
  }

  logout(): void {
    this.clearState();
    sessionStorage.removeItem('auth:return_url');
    window.location.assign(`${this.bffUrl}/auth/logout`);
  }

  handleUnauthorized(returnUrl: string = this.router.url): void {
    this.clearState();
    const sanitized = this.sanitizeReturnUrl(returnUrl);
    const fullReturnUrl = `${window.location.origin}${sanitized}`;
    window.location.href = `${this.bffUrl}/auth/login?returnUrl=${encodeURIComponent(fullReturnUrl)}`;
  }

  // ───────────────────────────────────────────────
  // Hybrid context switching
  // ───────────────────────────────────────────────
  /**
   * Switch active profile silently (student/tutor/admin)
   * Syncs backend session and refreshes local user state.
   */
  switchProfile(target: 'student' | 'tutor' | 'admin') {
    return this.http
      .post<{ activeProfile: string }>(
        `${this.bffUrl}/auth/switch-profile/${target}`,
        {},
        { withCredentials: true }
      )
      .pipe(
        tap(() => {
          console.info('[Auth] Switched profile →', target);
          this.refreshUser();
        }),
        catchError((err) => {
          console.error('[Auth] switchProfile failed:', err);
          return of(null);
        })
      );
  }

  /**
   * Re-fetches user context from BFF after switching profile.
   */
  refreshUser(): void {
    this.http
      .get<{ isAuthenticated: boolean } & UserProfile>(
        `${this.bffUrl}/auth/user`,
        { withCredentials: true }
      )
      .pipe(
        map((response) => {
          if (response.isAuthenticated) {
            const user: UserProfile = {
              userId: response.userId,
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
            this.setState({ isAuthenticated: true, user, error: null });
          } else {
            this.clearState();
          }
        }),
        catchError((err) => {
          console.error('[Auth] refreshUser error:', err);
          this.clearState();
          return of(null);
        })
      )
      .subscribe();
  }

  // ───────────────────────────────────────────────
  // Helpers / State Management
  // ───────────────────────────────────────────────
  private setState(patch: Partial<AuthState>): void {
    this.authState.update((s) => ({ ...s, ...patch }));
  }

  private clearState(): void {
    sessionStorage.removeItem('auth:return_url');
    this.authState.set({ isAuthenticated: false, user: null, error: null });
  }

  private sanitizeReturnUrl(url?: string): string {
    if (!url || url === '/') return '/';
    try {
      if (url.startsWith('/') && !url.startsWith('//')) return url;
      const parsed = new URL(url, window.location.origin);
      return parsed.origin === window.location.origin
        ? `${parsed.pathname}${parsed.search}${parsed.hash}`
        : '/';
    } catch {
      return '/';
    }
  }
}
