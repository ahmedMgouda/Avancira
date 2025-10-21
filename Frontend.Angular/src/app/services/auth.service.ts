import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, firstValueFrom, of, timeout } from 'rxjs';

import { AuthState, UserProfile } from '../core/models/auth.models';
import { environment } from '../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  /** Base BFF endpoint (from environment) */
  private readonly bffUrl = environment.baseUrl;
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

  // ----------------------------------------------------------
  // Reactive selectors (computed signals)
  // ----------------------------------------------------------
  readonly isAuthenticated = computed(() => this.authState().isAuthenticated);
  readonly currentUser = computed(() => this.authState().user);
  readonly roles = computed(() => this.authState().user?.roles ?? []);
  readonly permissions = computed(() => this.authState().user?.permissions ?? []);
  readonly authError = computed(() => this.authState().error);
  readonly ready = computed(() => this.initialized);

  // ----------------------------------------------------------
  // Initialization
  // ----------------------------------------------------------

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
          .get<{ isAuthenticated: boolean; user: UserProfile | null }>(
            `${this.bffUrl}/auth/user`,
            { withCredentials: true }
          )
          .pipe(
            timeout(15000),
            catchError((err) => {
              console.warn('[Auth] Failed to load user session:', err);
              return of({ isAuthenticated: false, user: null });
            })
          )
      );

      if (response.isAuthenticated && response.user) {
        this.setState({
          isAuthenticated: true,
          user: response.user,
          error: null,
        });
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

  // ----------------------------------------------------------
  // Login / Logout
  // ----------------------------------------------------------

  /**
   * Starts login via BFF (redirects to MVC login page)
   */

  startLogin(returnUrl: string = this.router.url): void {
    const sanitized = this.sanitizeReturnUrl(returnUrl);
    sessionStorage.setItem('auth:return_url', sanitized);

    const fullReturnUrl = `${window.location.origin}${sanitized}`;

    window.location.href = `${this.bffUrl}/auth/login?returnUrl=${encodeURIComponent(
      fullReturnUrl
    )}`;
  }


  /**
   * Logs out via BFF (revokes session and redirects)
   */
  logout(): void {
    sessionStorage.removeItem('auth:return_url');

    this.http
      .post<{ redirectUri?: string }>(`${this.bffUrl}/auth/logout`, {})
      .pipe(
        catchError((error) => {
          console.error('[Auth] Logout failed:', error);
          return of<{ redirectUri?: string }>({ redirectUri: undefined });
        })
      )
      .subscribe(({ redirectUri }) => {
        this.clearState();
        // Prefer server-provided redirectUri, fallback to login
        window.location.href =
          redirectUri ?? `${this.bffUrl}/auth/login`;
      });
  }

  /**
   * Called by interceptor when unauthorized
   */
  handleUnauthorized(returnUrl: string = this.router.url): void {
    this.clearState();
    const sanitized = this.sanitizeReturnUrl(returnUrl);
    const fullReturnUrl = `${window.location.origin}${sanitized}`;

    window.location.href = `${this.bffUrl}/auth/login?returnUrl=${encodeURIComponent(
      fullReturnUrl
    )}`;
  }
  // ----------------------------------------------------------
  // Helpers / State Management
  // ----------------------------------------------------------

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
