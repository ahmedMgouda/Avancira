import { HttpClient, HttpContext, HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, OnDestroy, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { Router, UrlTree } from '@angular/router';
import {
  defer,
  from,
  Observable,
  of,
  ReplaySubject,
  Subject,
  throwError,
  timer,
} from 'rxjs';
import {
  catchError,
  distinctUntilChanged,
  finalize,
  map,
  retry,
  shareReplay,
  switchMap,
  take,
  takeUntil,
  tap,
  timeout,
} from 'rxjs/operators';
import { AuthConfig, OAuthEvent, OAuthService } from 'angular-oauth2-oidc';

import { SessionService } from './session.service';

import { environment } from '../environments/environment';
import { INCLUDE_CREDENTIALS, SKIP_AUTH } from '../interceptors/auth.interceptor';
import { RegisterUserRequest } from '../models/register-user-request';
import { RegisterUserResponseDto } from '../models/register-user-response';
import { UserProfile } from '../models/UserProfile';

// --- Types ---
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
  iat?: number | string;
  nbf?: number | string;
  aud?: string | string[];
  iss?: string;
  [key: string]: unknown;
}

export interface AuthState {
  isAuthenticated: boolean;
  user: UserProfile | null;
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
  REGISTRATION_FAILED = 'REGISTRATION_FAILED',
  NETWORK_ERROR = 'NETWORK_ERROR',
  DISCOVERY_FAILED = 'DISCOVERY_FAILED',
  INVALID_TOKEN = 'INVALID_TOKEN',
}

export interface AuthError {
  type: AuthErrorType;
  message: string;
  timestamp: number;
  originalError?: any;
  retryable: boolean;
  userMessage?: string;
}

// --- Defaults & Constants ---
const defaultAuthState: AuthState = {
  isAuthenticated: false,
  user: null,
  isLoading: false,
  error: null,
  tokenExpiresAt: null,
  refreshInProgress: false,
};

const TOKEN_REFRESH_BUFFER_MS = 5 * 60 * 1000;     // 5 minutes
const TOKEN_EXPIRY_WARNING_MS = 10 * 60 * 1000;    // 10 minutes
const MIN_TOKEN_LIFETIME_MS = 60 * 1000;
const MAX_RETRY_ATTEMPTS = 3;
const RETRY_BASE_DELAY_MS = 1000;
const NETWORK_TIMEOUT_MS = 15000;

const OAUTH_STORAGE_KEYS = [
  'access_token',
  'refresh_token',
  'id_token',
  'nonce',
  'state',
  'code_verifier',
  'PKCE_verifier',
  'auth_return_url',
  'auth_config',
] as const;

@Injectable({ providedIn: 'root' })
export class AuthService implements OnDestroy {
  private readonly api = environment.apiUrl;
  private readonly destroy$ = new Subject<void>();

  private refresh$?: Observable<string>;
  private tokenExpiryTimer?: ReturnType<typeof setTimeout>;
  private tokenExpiryWarningTimer?: ReturnType<typeof setTimeout>;
  private initPromise?: Promise<void>;
  private readonly tokenExpiringSoon$ = new ReplaySubject<void>(1);
  private redirectingToSignIn = false;

  // --- State ---
  private readonly authState = signal<AuthState>({ ...defaultAuthState });

  // --- Computed ---
  readonly isAuthenticated = computed(() => this.authState().isAuthenticated);
  readonly currentUser = computed(() => this.authState().user);
  readonly isLoading = computed(() => this.authState().isLoading);
  readonly authError = computed(() => this.authState().error);
  readonly tokenExpiresAt = computed(() => this.authState().tokenExpiresAt);
  readonly refreshInProgress = computed(() => this.authState().refreshInProgress);

  // --- Observables ---
  readonly authState$ = toObservable(this.authState).pipe(
    distinctUntilChanged((a, b) => this.authStatesEqual(a, b))
  );

  readonly isAuthenticated$ = this.authState$.pipe(
    map((s) => s.isAuthenticated),
    distinctUntilChanged()
  );

  readonly currentUser$ = this.authState$.pipe(
    map((s) => s.user),
    distinctUntilChanged((a, b) => this.usersEqual(a, b))
  );

  readonly authError$ = this.authState$.pipe(
    map((s) => s.error),
    distinctUntilChanged()
  );

  readonly tokenExpiring$ = this.tokenExpiringSoon$.asObservable();

  // --- Dependencies ---
  private readonly oauth = inject(OAuthService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly sessionService = inject(SessionService);

  constructor() {
    this.configureOAuth();
    this.setupEventListeners();
  }

  ngOnDestroy(): void {
    this.resetAuth();
    this.destroy$.next();
    this.destroy$.complete();
    this.tokenExpiringSoon$.complete(); // ✅ prevent ReplaySubject leak
  }

  // ------------------
  // Public API
  // ------------------

  async init(): Promise<void> {
    if (this.initPromise) return this.initPromise;
    this.initPromise = this.performInit();
    return this.initPromise;
  }

  getAccessToken(): string | null {
    return this.oauth.getAccessToken();
  }

  getValidAccessToken(): Observable<string> {
    const token = this.oauth.getAccessToken();
    if (this.isAuthenticated() && this.isTokenValid() && token) return of(token);
    if (this.refresh$) return this.refresh$;
    this.refresh$ = this.createRefreshTokenObservable();
    return this.refresh$;
  }

  waitForRefresh(): Observable<string | null> {
    return this.refresh$ ?? of(null); // ✅ safer than EMPTY/NEVER
  }

  startLogin(returnUrl: string = this.router.url): void {
    try {
      const sanitized = this.sanitizeReturnUrl(returnUrl);
      sessionStorage.setItem('auth_return_url', sanitized);
      this.setAuthState({ isLoading: true, error: null });
      this.oauth.initCodeFlow(sanitized);
    } catch (error) {
      this.failWithError(AuthErrorType.LOGIN_FAILED, 'Failed to start login process', error, true);
    }
  }

  logout(clearOnly = false): Observable<void> {
    const sessionId = this.currentUser()?.sessionId;
    this.setAuthState({ isLoading: true });

    const revoke$ = !clearOnly && sessionId
      ? this.sessionService.revokeSession(sessionId).pipe(
        timeout(5000),
        catchError(() => of(void 0))
      )
      : of(void 0);

    return revoke$.pipe(
      tap(() => this.performLogout(clearOnly)),
      finalize(() => this.setAuthState({ isLoading: false })),
      takeUntil(this.destroy$)
    );
  }

  register(data: RegisterUserRequest): Observable<RegisterUserResponseDto> {
    return this.http
      .post<RegisterUserResponseDto>(`${this.api}/users/register`, data, {
        context: new HttpContext().set(SKIP_AUTH, true).set(INCLUDE_CREDENTIALS, true),
      })
      .pipe(
        timeout(NETWORK_TIMEOUT_MS),
        retry({ count: 2, delay: (_, i) => timer(i * 2000) }),
        catchError((error: HttpErrorResponse) =>
          throwError(() =>
            this.createAuthError(
              AuthErrorType.REGISTRATION_FAILED,
              this.getErrorMessage(error),
              error,
              this.isRetryableError(error),
              'Registration failed. Please try again.'
            )
          )
        )
      );
  }

  getUserProfile(): UserProfile | null {
    return this.decodeTokenClaims();
  }

  redirectToSignIn(returnUrl: string = this.router.url): UrlTree {
    return this.router.createUrlTree(['/signin'], {
      queryParams: { returnUrl: this.sanitizeReturnUrl(returnUrl) },
    });
  }

  handleUnauthorized(returnUrl: string = this.router.url): void {
    if (this.redirectingToSignIn) return;

    const sanitized = this.sanitizeReturnUrl(returnUrl);
    this.redirectingToSignIn = true;

    sessionStorage.setItem('auth_return_url', sanitized);
    this.resetAuth();

    const target = this.redirectToSignIn(sanitized);
    queueMicrotask(() => {
      void this.router.navigateByUrl(target).finally(() => {
        this.redirectingToSignIn = false;
      });
    });
  }

  // ------------------
  // Private helpers
  // ------------------

  private async performInit(): Promise<void> {
    try {
      this.setAuthState({ isLoading: true });

      const success = await this.oauth.loadDiscoveryDocumentAndTryLogin({
        onTokenReceived: () => {
          this.updateAuthState();
          this.setupTokenExpiryTimer();
        },
      });

      if (!success) throw new Error('Discovery document loading or login failed');

      if (this.oauth.getRefreshToken()) this.setupTokenExpiryTimer();

      this.updateAuthState();
    } catch (error) {
      this.failWithError(AuthErrorType.INITIALIZATION_FAILED, 'Auth initialization failed', error, true);
    } finally {
      this.setAuthState({ isLoading: false });
    }
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
      timeoutFactor: 0.75,
      sessionChecksEnabled: true,
      checkOrigin: true,
      requireHttps: environment.production,
    };
    this.oauth.configure(authConfig);
    this.oauth.setStorage(sessionStorage);
  }

  private setupEventListeners(): void {
    this.oauth.events.pipe(takeUntil(this.destroy$)).subscribe((event: OAuthEvent) => {
      switch (event.type) {
        case 'token_received':
        case 'token_refreshed':
          this.updateAuthState();
          this.setupTokenExpiryTimer();
          this.setAuthError(null);
          break;
        case 'token_error':
        case 'token_refresh_error':
          this.failWithError(AuthErrorType.TOKEN_EXPIRED, 'Token expired or invalid', event, true);
          break;
        case 'logout':
        case 'session_terminated':
          this.resetAuth();
          break;
      }
    });
  }

  private setupTokenExpiryTimer(): void {
    this.stopTokenTimers();

    const user = this.currentUser();
    if (!user?.exp) return;

    const expiresAt = user.exp * 1000;
    const now = Date.now();
    const timeUntilRefresh = expiresAt - now - TOKEN_REFRESH_BUFFER_MS;
    const timeUntilWarning = expiresAt - now - TOKEN_EXPIRY_WARNING_MS;

    this.setAuthState({ tokenExpiresAt: expiresAt });

    if (timeUntilWarning > 0) {
      this.tokenExpiryWarningTimer = setTimeout(
        () => this.tokenExpiringSoon$.next(),
        timeUntilWarning
      );
    }

    if (timeUntilRefresh > MIN_TOKEN_LIFETIME_MS) {
      this.tokenExpiryTimer = setTimeout(() => {
        if (this.isAuthenticated() && this.oauth.getRefreshToken()) {
          this.getValidAccessToken().pipe(take(1)).subscribe();
        }
      }, timeUntilRefresh);
    }
  }

  private createRefreshTokenObservable(): Observable<string> {
    this.setAuthState({ refreshInProgress: true });

    return defer(() => {
      if (!this.oauth.getRefreshToken()) {
        throw this.createAuthError(AuthErrorType.REFRESH_FAILED, 'No refresh token available');
      }
      return from(this.oauth.refreshToken());
    }).pipe(
      timeout(NETWORK_TIMEOUT_MS),
      retry({ count: MAX_RETRY_ATTEMPTS, delay: (_, i) => timer(Math.pow(2, i) * RETRY_BASE_DELAY_MS) }),
      switchMap(() => {
        const token = this.oauth.getAccessToken();
        if (!token) {
          throw this.createAuthError(AuthErrorType.REFRESH_FAILED, 'No access token after refresh');
        }
        this.updateAuthState();
        this.setupTokenExpiryTimer();
        return of(token);
      }),
      catchError((error) => {
        const authError = this.createAuthError(AuthErrorType.REFRESH_FAILED, 'Token refresh failed', error);
        this.setAuthError(authError);
        this.performLogout(true);
        return throwError(() => authError);
      }),
      finalize(() => {
        this.refresh$ = undefined;
        this.setAuthState({ refreshInProgress: false });
      }),
      shareReplay({ bufferSize: 1, refCount: false })
    );
  }

  private performLogout(clearOnly: boolean): void {
    try {
      const arg = clearOnly
        ? true
        : { logoutUrl: `${environment.baseApiUrl}/connect/logout`, postLogoutRedirectUri: environment.postLogoutRedirectUri };
      this.oauth.logOut(arg);
    } finally {
      this.clearAuthStorage();
      this.clearAuthState();
      this.stopTokenTimers();
      this.refresh$ = undefined;
      this.initPromise = undefined;
    }
  }

  private stopTokenTimers(): void {
    if (this.tokenExpiryTimer) clearTimeout(this.tokenExpiryTimer);
    if (this.tokenExpiryWarningTimer) clearTimeout(this.tokenExpiryWarningTimer);
    this.tokenExpiryTimer = this.tokenExpiryWarningTimer = undefined;
  }

  private clearAuthStorage(): void {
    OAUTH_STORAGE_KEYS.forEach((key) => sessionStorage.removeItem(key));
  }

  private setAuthState(partial: Partial<AuthState>): void {
    this.authState.update((s) => ({ ...s, ...partial }));
  }

  private clearAuthState(): void {
    this.authState.set({ ...defaultAuthState });
  }

  private setAuthError(error: AuthError | null): void {
    this.authState.update((s) => ({ ...s, error, isLoading: false }));
  }

  private updateAuthState(): void {
    const claims = this.decodeTokenClaims();
    this.setAuthState({
      isAuthenticated: !!claims,
      user: claims,
      isLoading: false,
      error: null,
      tokenExpiresAt: claims?.exp ? claims.exp * 1000 : null,
    });
  }

  // --- Token claim decoding ---
  private decodeTokenClaims(): UserProfile | null {
    try {
      const claims = this.oauth.getIdentityClaims() as IdTokenClaims | null;
      if (!claims?.sub) return null;

      return {
        id: claims.sub,
        email: this.getString(claims, 'email'),
        firstName: this.getString(claims, 'given_name'),
        lastName: this.getString(claims, 'family_name'),
        fullName: this.getString(claims, 'name'),
        timeZoneId: this.getString(claims, 'timezone'),
        ipAddress: this.getString(claims, 'ip_address'),
        imageUrl: this.getString(claims, 'image'),
        deviceId: this.getString(claims, 'device_id'),
        sessionId: this.getString(claims, 'sid') || this.getString(claims, 'session_id'),
        country: this.getString(claims, 'country'),
        city: this.getString(claims, 'city'),
        roles: this.getArray(claims, 'role'),
        permissions: this.getArray(claims, 'permission'),
        exp: this.getNumber(claims, 'exp'),
      };
    } catch {
      return null;
    }
  }

  // --- Claim parsing helpers ---
  private getString(c: IdTokenClaims, k: keyof IdTokenClaims, fallback = ''): string {
    const v = c[k];
    return typeof v === 'string' && v.trim() ? v.trim() : fallback;
  }

  private getNumber(c: IdTokenClaims, k: keyof IdTokenClaims, fallback = 0): number {
    const v = c[k];
    if (typeof v === 'number') return v;
    if (typeof v === 'string') {
      const n = Number(v);
      return isNaN(n) ? fallback : n;
    }
    return fallback;
  }

  private getArray(c: IdTokenClaims, k: keyof IdTokenClaims): string[] {
    const v = c[k];
    if (Array.isArray(v)) {
      return v.filter((x): x is string => typeof x === 'string' && x.trim().length > 0);
    }
    if (typeof v === 'string' && v.trim().length > 0) {
      return [v.trim()];
    }
    return [];
  }

  // --- Error helpers ---
  private failWithError(
    type: AuthErrorType,
    message: string,
    originalError?: any,
    retryable = false,
    userMessage?: string
  ): void {
    const error = this.createAuthError(type, message, originalError, retryable, userMessage);
    if (!environment.production) console.error('[AuthService]', error);
    this.setAuthError(error);
  }

  private createAuthError(
    type: AuthErrorType,
    message: string,
    originalError?: any,
    retryable = false,
    userMessage?: string
  ): AuthError {
    return { type, message, timestamp: Date.now(), originalError, retryable, userMessage };
  }

  // --- Equality helpers ---
  private errorsEqual(a: AuthError | null, b: AuthError | null): boolean {
    if (a === b) return true;
    if (!a || !b) return false;
    return a.type === b.type && a.message === b.message && a.retryable === b.retryable;
  }

  private usersEqual(a: UserProfile | null, b: UserProfile | null): boolean {
    if (a === b) return true;
    if (!a || !b) return false;
    return (
      a.id === b.id &&
      a.email === b.email &&
      a.sessionId === b.sessionId &&
      a.exp === b.exp &&
      a.firstName === b.firstName &&
      a.lastName === b.lastName
    );
  }

  private authStatesEqual(a: AuthState, b: AuthState): boolean {
    return (
      a.isAuthenticated === b.isAuthenticated &&
      a.isLoading === b.isLoading &&
      a.refreshInProgress === b.refreshInProgress &&
      a.tokenExpiresAt === b.tokenExpiresAt &&
      this.errorsEqual(a.error, b.error) &&
      this.usersEqual(a.user, b.user)
    );
  }

  private resetAuth(): void {
    this.clearAuthState();
    this.stopTokenTimers();
    this.clearAuthStorage();
    this.refresh$ = undefined;
    this.initPromise = undefined;
  }

  private isTokenValid(): boolean {
    const expiresAt = this.tokenExpiresAt();
    return !!expiresAt && Date.now() < expiresAt - TOKEN_REFRESH_BUFFER_MS;
  }

  private isRetryableError(error: HttpErrorResponse): boolean {
    return (
      error.status === 0 ||     // network
      error.status === 408 ||
      error.status === 429 ||
      (error.status >= 500 && error.status < 600)
    );
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    const e = error.error ?? {};
    return (
      e.message ??
      e.error_description ??
      e.detail ??
      (error.message !== 'Unknown Error' ? error.message : null) ??
      `HTTP ${error.status}: ${error.statusText}`
    );
  }

  private sanitizeReturnUrl(url: string): string {
    try {
      if (!url || url === '/') return '/';
      if (url.startsWith('/') && !url.startsWith('//')) return url;
      const parsed = new URL(url, window.location.origin);
      return parsed.origin === window.location.origin
        ? parsed.pathname + parsed.search + parsed.hash
        : '/';
    } catch {
      return '/';
    }
  }
}
