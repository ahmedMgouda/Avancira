import { HttpClient, HttpContext, HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom, forkJoin, Observable, of, throwError } from 'rxjs';
import { catchError, map, switchMap, tap } from 'rxjs/operators';

import {
  AuthError,
  AuthErrorType,
  AuthState,
  PermissionsResponse,
  UserProfile,
  UserProfileResponse,
} from '../core/models/auth.models';
import { environment } from '../environments/environment';
import { INCLUDE_CREDENTIALS } from '../interceptors/auth.interceptor';

interface BffUserResponse {
  isAuthenticated: boolean;
  sub?: string;
  name?: string;
  givenName?: string;
  familyName?: string;
  email?: string;
  emailVerified?: boolean;
  roles?: string[];
  scopes?: string[];
  tokenExpiresAt?: string;
  tokenExpiresIn?: number;
}

const SESSION_STORAGE_KEYS = {
  RETURN_URL: 'auth:return_url',
} as const;

const defaultAuthState: AuthState = {
  isAuthenticated: false,
  user: null,
  roles: [],
  permissions: [],
  isLoading: false,
  error: null,
  tokenExpiresAt: null,
  refreshInProgress: false,
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = environment.apiUrl;
  private readonly authUrl = environment.authUrl;

  private initPromise?: Promise<void>;

  private readonly authState = signal<AuthState>({ ...defaultAuthState });

  readonly isAuthenticated = computed(() => this.authState().isAuthenticated);
  readonly currentUser = computed(() => this.authState().user);
  readonly roles = computed(() => this.authState().roles);
  readonly permissions = computed(() => this.authState().permissions);
  readonly isLoading = computed(() => this.authState().isLoading);
  readonly authError = computed(() => this.authState().error);
  readonly tokenExpiresAt = computed(() => this.authState().tokenExpiresAt);
  readonly refreshInProgress = computed(() => this.authState().refreshInProgress);

  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  async init(): Promise<void> {
    if (this.initPromise) {
      return this.initPromise;
    }

    this.initPromise = this.performInit();
    return this.initPromise;
  }

  async startLogin(returnUrl: string = this.router.url): Promise<void> {
    const sanitized = this.sanitizeReturnUrl(returnUrl);
    sessionStorage.setItem(SESSION_STORAGE_KEYS.RETURN_URL, sanitized);

    const callbackPath = `/auth/callback?returnUrl=${encodeURIComponent(sanitized)}`;
    const loginUrl = `${this.authUrl}/login?returnUrl=${encodeURIComponent(callbackPath)}`;

    window.location.href = loginUrl;
  }

  handleAuthCallback(returnUrl?: string): Observable<void> {
    const stored = sessionStorage.getItem(SESSION_STORAGE_KEYS.RETURN_URL);
    const target = this.sanitizeReturnUrl(returnUrl ?? stored ?? '/');

    return this.refreshSession().pipe(
      tap((isAuthenticated) => {
        sessionStorage.removeItem(SESSION_STORAGE_KEYS.RETURN_URL);
        if (isAuthenticated) {
          void this.router.navigateByUrl(target);
        } else {
          this.handleUnauthorized(target);
        }
      }),
      map(() => void 0)
    );
  }

  logout(): void {
    this.http
      .post(`${this.authUrl}/logout`, {}, { context: this.credentialsContext() })
      .pipe(
        catchError((error) => {
          this.failWithError(
            AuthErrorType.LOGOUT_FAILED,
            'Failed to logout from the session',
            error,
            this.isRetryableError(error)
          );
          return throwError(() => error);
        })
      )
      .subscribe({
        next: () => {
          this.clearAuthState();
          void this.router.navigate(['/signin']);
        },
        error: () => {
          this.clearAuthState();
          void this.router.navigate(['/signin']);
        },
      });
  }

  handleUnauthorized(returnUrl: string = this.router.url): void {
    const sanitized = this.sanitizeReturnUrl(returnUrl);
    sessionStorage.setItem(SESSION_STORAGE_KEYS.RETURN_URL, sanitized);
    this.clearAuthState();
    void this.router.navigate(['/signin'], {
      queryParams: { returnUrl: sanitized },
    });
  }

  private async performInit(): Promise<void> {
    try {
      this.setAuthState({ isLoading: true });
      await firstValueFrom(this.refreshSession());
    } catch (error) {
      this.failWithError(
        AuthErrorType.INITIALIZATION_FAILED,
        'Failed to initialize authentication',
        error,
        this.isRetryableError(error)
      );
      this.clearAuthState();
    } finally {
      this.setAuthState({ isLoading: false });
    }
  }

  private refreshSession(): Observable<boolean> {
    return this.fetchUserContext().pipe(
      switchMap((user) => {
        if (!user.isAuthenticated) {
          this.clearAuthState();
          return of(false);
        }

        return this.loadUserProfile(user);
      })
    );
  }

  private fetchUserContext(): Observable<BffUserResponse> {
    return this.http
      .get<BffUserResponse>(`${this.authUrl}/user`, {
        context: this.credentialsContext(),
      })
      .pipe(
        catchError((error) => {
          this.failWithError(
            AuthErrorType.NETWORK_ERROR,
            'Failed to contact authentication service',
            error,
            this.isRetryableError(error)
          );
          return throwError(() => error);
        })
      );
  }

  private loadUserProfile(user: BffUserResponse): Observable<boolean> {
    const permissions$ = this.http.get<PermissionsResponse>(`${this.apiUrl}/users/permissions`, {
      context: this.credentialsContext(),
    });

    const profile$ = this.http.get<UserProfileResponse>(`${this.apiUrl}/users/profile`, {
      context: this.credentialsContext(),
    });

    return forkJoin({ permResponse: permissions$, profile: profile$ }).pipe(
      tap(({ permResponse, profile }) => {
        const userProfile: UserProfile = {
          id: user.sub ?? profile.id,
          email: user.email ?? profile.email ?? '',
          firstName: user.givenName ?? profile.firstName ?? '',
          lastName: user.familyName ?? profile.lastName ?? '',
          fullName: user.name ?? profile.fullName ?? '',
          imageUrl: profile.imageUrl ?? '',
          roles: user.roles ?? [],
        };

        this.setAuthState({
          isAuthenticated: true,
          user: userProfile,
          permissions: permResponse.permissions ?? [],
          roles: user.roles ?? [],
          tokenExpiresAt: user.tokenExpiresAt ? Date.parse(user.tokenExpiresAt) : null,
          error: null,
        });
      }),
      map(() => true),
      catchError((error) => {
        this.failWithError(
          AuthErrorType.NETWORK_ERROR,
          'Failed to load user profile',
          error,
          this.isRetryableError(error)
        );
        return throwError(() => error);
      })
    );
  }

  private credentialsContext(): HttpContext {
    return new HttpContext().set(INCLUDE_CREDENTIALS, true);
  }

  private clearAuthState(): void {
    this.authState.set({ ...defaultAuthState });
  }

  private setAuthState(partial: Partial<AuthState>): void {
    this.authState.update((state) => ({ ...state, ...partial }));
  }

  private failWithError(
    type: AuthErrorType,
    message: string,
    originalError?: unknown,
    retryable = false
  ): void {
    const error: AuthError = {
      type,
      message,
      timestamp: Date.now(),
      originalError,
      retryable,
    };

    if (!environment.production) {
      console.error('[AuthService]', error);
    }

    this.setAuthState({ error, isLoading: false });
  }

  private isRetryableError(error: unknown): boolean {
    if (!(error instanceof HttpErrorResponse)) {
      return false;
    }

    return (
      error.status === 0 ||
      error.status === 408 ||
      error.status === 429 ||
      (error.status >= 500 && error.status < 600)
    );
  }

  private sanitizeReturnUrl(url?: string): string {
    if (!url || url === '/') {
      return '/';
    }

    try {
      if (url.startsWith('/') && !url.startsWith('//')) {
        return url;
      }

      const base = new URL(environment.frontendUrl);
      const parsed = new URL(url, base);
      return parsed.origin === base.origin
        ? `${parsed.pathname}${parsed.search}${parsed.hash}`
        : '/';
    } catch {
      return '/';
    }
  }
}
