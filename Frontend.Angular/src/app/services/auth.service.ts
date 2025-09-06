import { HttpClient, HttpContext, HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, OnDestroy, signal } from '@angular/core';
import { Router, UrlTree } from '@angular/router';
import {
  defer,
  from,
  Observable,
  of,
  Subject,
  throwError,
  timer
} from 'rxjs';
import {
  catchError,
  finalize,
  retry,
  shareReplay,
  switchMap,
  takeUntil,
  tap,
  timeout
} from 'rxjs/operators';
import { AuthConfig, OAuthEvent, OAuthService } from 'angular-oauth2-oidc';

import { SessionService } from './session.service';

import { environment } from '../environments/environment';
import { INCLUDE_CREDENTIALS, SKIP_AUTH } from '../interceptors/auth.interceptor';
import { RegisterUserRequest } from '../models/register-user-request';
import { RegisterUserResponseDto } from '../models/register-user-response';
import { UserProfile } from '../models/UserProfile';

// Strongly typed ID token claims
interface IdTokenClaims {
  sub: string;
  email?: string;
  given_name?: string;
  family_name?: string;
  name?: string;
  timezone?: string;
  ip_address?: string;
  image?: string;
  device_id?: string;
  sid?: string;
  session_id?: string;
  country?: string;
  city?: string;
  role?: string | string[];
  permission?: string | string[];
  exp?: number | string;
  [key: string]: unknown;
}

export interface AuthState {
  isAuthenticated: boolean;
  user: UserProfile | null;
  isLoading: boolean;
  error: string | null;
}

@Injectable({ providedIn: 'root' })
export class AuthService implements OnDestroy {
  private readonly api = environment.apiUrl;
  private readonly destroy$ = new Subject<void>();
  private refresh$?: Observable<string>;

  // Signals for reactive state management
  private readonly authState = signal<AuthState>({
    isAuthenticated: false,
    user: null,
    isLoading: false,
    error: null
  });

  // Public readonly signals
  readonly isAuthenticated = computed(() => this.authState().isAuthenticated);
  readonly currentUser = computed(() => this.authState().user);
  readonly isLoading = computed(() => this.authState().isLoading);
  readonly authError = computed(() => this.authState().error);

  private readonly oauth = inject(OAuthService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly sessionService = inject(SessionService);

  constructor() {
    this.configureOAuth();
    this.setupEventListeners();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private configureOAuth(): void {
    const authConfig: AuthConfig = {
      issuer: environment.baseApiUrl,
      clientId: environment.clientId,
      responseType: 'code',
      scope: 'openid profile email offline_access',
      redirectUri: environment.redirectUri,
      postLogoutRedirectUri: environment.postLogoutRedirectUri,
      strictDiscoveryDocumentValidation: true,
      showDebugInformation: !environment.production,
      clearHashAfterLogin: true,
      nonceStateSeparator: 'semicolon',
      timeoutFactor: 0.75, // proactive refresh
    };

    this.oauth.configure(authConfig);
    this.oauth.setStorage(sessionStorage); // safer than localStorage
  }

  private setupEventListeners(): void {
    this.oauth.events
      .pipe(takeUntil(this.destroy$))
      .subscribe((event: OAuthEvent) => {
        switch (event.type) {
          case 'token_received':
          case 'token_refreshed':
            this.updateAuthState();
            break;
          case 'token_error':
            this.handleAuthError('Token expired or invalid');
            break;
          case 'logout':
          case 'session_terminated':
            this.clearAuthState();
            break;
        }
      });
  }

  /** Initialize authentication - call once at app startup */
  async init(): Promise<void> {
    try {
      this.setLoading(true);

      const success = await this.oauth.loadDiscoveryDocumentAndTryLogin({
        onTokenReceived: (ctx) => {
          console.info('✅ Token received', ctx);
          this.updateAuthState();
        }
      });

      if (success && this.oauth.getRefreshToken()) {
        this.oauth.setupAutomaticSilentRefresh({
          checkInterval: 60, // 5 minutes
        });
      }

      this.updateAuthState();
    } catch (error) {
      console.error('❌ Auth initialization failed', error);
      this.handleAuthError('Authentication initialization failed');
    } finally {
      this.setLoading(false);
    }
  }

  isUserAuthenticated(): boolean {
    return this.oauth.hasValidAccessToken() && this.oauth.hasValidIdToken();
  }

  waitForRefresh(): Observable<unknown> {
    return this.refresh$ ?? of(null);
  }

  getAccessToken(): string | null {
    return this.oauth.getAccessToken();
  }

  /** Get valid access token or refresh it */
  getValidAccessToken(): Observable<string> {
    if (this.isUserAuthenticated()) {
      return of(this.oauth.getAccessToken()!);
    }

    if (this.refresh$) {
      return this.refresh$;
    }

    this.refresh$ = this.createRefreshTokenObservable();
    return this.refresh$;
  }

  private createRefreshTokenObservable(): Observable<string> {
    return defer(() => {
      if (!this.oauth.getRefreshToken()) {
        return throwError(() => new Error('No refresh token available'));
      }
      return from(this.oauth.refreshToken());
    }).pipe(
      timeout(15000),
      retry({
        count: 2,
        delay: (_, retryCount) => {
          console.warn(`🔄 Token refresh retry ${retryCount}`);
          return timer(retryCount * 1000);
        }
      }),
      switchMap(() => {
        const token = this.oauth.getAccessToken();
        if (!token) throw new Error('No access token after refresh');
        this.updateAuthState();
        return of(token);
      }),
      catchError((error) => {
        console.error('❌ Token refresh failed permanently', error);
        this.handleTokenRefreshError(error);
        return throwError(() => error);
      }),
      finalize(() => {
        this.refresh$ = undefined;
      }),
      shareReplay({ bufferSize: 1, refCount: false })
    );
  }

  startLogin(returnUrl: string = this.router.url): void {
    try {
      sessionStorage.setItem('auth_return_url', returnUrl);
      this.setLoading(true);
      this.oauth.initCodeFlow(returnUrl);
    } catch (error) {
      console.error('❌ Login initiation failed', error);
      this.handleAuthError('Failed to start login process');
      this.setLoading(false);
    }
  }

  logout(clearOnly: boolean = false): Observable<void> {
    const currentUser = this.currentUser();
    const sessionId = currentUser?.sessionId;

    this.setLoading(true);

    const revoke$ = !clearOnly && sessionId
      ? this.sessionService.revokeSession(sessionId).pipe(
        catchError((error) => {
          console.warn('⚠️ Session revocation failed', error);
          return of(void 0);
        })
      )
      : of(void 0);

    return revoke$.pipe(
      tap(() => this.performLogout(clearOnly)),
      finalize(() => this.setLoading(false)),
      takeUntil(this.destroy$)
    );
  }

  private performLogout(clearOnly: boolean): void {
    try {
      this.oauth.logOut(clearOnly || {
        logoutUrl: `${environment.baseApiUrl}connect/logout`,
        postLogoutRedirectUri: environment.postLogoutRedirectUri,
      });

      // Clear all auth storage
      sessionStorage.clear();
      this.clearAuthState();
    } catch (error) {
      console.error('❌ Logout failed', error);
      this.clearAuthState();
    }
  }

  register(data: RegisterUserRequest): Observable<RegisterUserResponseDto> {
    return this.http.post<RegisterUserResponseDto>(
      `${this.api}/users/register`,
      data,
      {
        context: new HttpContext()
          .set(SKIP_AUTH, true)
          .set(INCLUDE_CREDENTIALS, true),
      }
    ).pipe(
      catchError((error: HttpErrorResponse) => {
        console.error('❌ Registration failed', error);
        return throwError(() => error);
      })
    );
  }

  getUserProfile(): UserProfile | null {
    return this.decodeTokenClaims();
  }

  redirectToSignIn(returnUrl: string = this.router.url): UrlTree {
    return this.router.createUrlTree(['/signin'], { queryParams: { returnUrl } });
  }

  // Helpers

  private updateAuthState(): void {
    const isAuth = this.isUserAuthenticated();
    const user = isAuth ? this.decodeTokenClaims() : null;

    this.authState.set({
      isAuthenticated: isAuth,
      user,
      isLoading: false,
      error: null
    });
  }

  private clearAuthState(): void {
    this.authState.set({
      isAuthenticated: false,
      user: null,
      isLoading: false,
      error: null
    });
  }

  private setLoading(isLoading: boolean): void {
    this.authState.update((state) => ({ ...state, isLoading }));
  }

  private handleAuthError(errorMessage: string): void {
    this.authState.update((state) => ({
      ...state,
      error: errorMessage,
      isLoading: false
    }));
  }

  private handleTokenRefreshError(_error: any): void {
    this.handleAuthError('Session expired. Please log in again.');
    setTimeout(() => this.performLogout(true), 2000); // give user feedback
  }

  private normalizeClaim(value: unknown): string[] {
    if (Array.isArray(value)) {
      return value.filter((v): v is string => typeof v === 'string' && v.trim().length > 0);
    }
    return typeof value === 'string' && value.trim() ? [value.trim()] : [];
  }

  private decodeTokenClaims(): UserProfile | null {
    const claims = this.oauth.getIdentityClaims() as IdTokenClaims | null;
    if (!claims?.sub) {
      console.warn('⚠️ Invalid or missing identity claims');
      return null;
    }

    return {
      id: claims.sub,
      email: claims.email ?? '',
      firstName: claims.given_name ?? '',
      lastName: claims.family_name ?? '',
      fullName: claims.name ?? '',
      timeZoneId: claims.timezone ?? '',
      ipAddress: claims.ip_address ?? '',
      imageUrl: claims.image ?? '',
      deviceId: claims.device_id ?? '',
      sessionId: claims.sid ?? claims.session_id ?? '',
      country: claims.country ?? '',
      city: claims.city ?? '',
      roles: this.normalizeClaim(claims.role),
      permissions: this.normalizeClaim(claims.permission),
      exp: typeof claims.exp === 'number' ? claims.exp : Number(claims.exp ?? 0),
    };
  }
}
