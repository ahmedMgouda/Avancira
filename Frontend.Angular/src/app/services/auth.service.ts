import { HttpClient, HttpContext, HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, OnDestroy, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import {
  defer,
  firstValueFrom,
  forkJoin,
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

import { AuthError, AuthErrorType, AuthState, PermissionsResponse, TokenResponse, UserInfoResponse, UserProfile, UserProfileResponse } from '../core/models/auth.models';
import { getJwtExpSeconds } from '../core/utils/jwt.util';
import { createPkcePair, generateRandomState } from '../core/utils/pkce.util';
import { environment } from '../environments/environment';
import { INCLUDE_CREDENTIALS, SKIP_AUTH } from '../interceptors/auth.interceptor';

const SESSION_STORAGE_KEYS = {
  ACCESS_TOKEN: 'auth:access_token',
  REFRESH_TOKEN: 'auth:refresh_token',
  TOKEN_EXPIRES_AT: 'auth:token_expires_at',
  RETURN_URL: 'auth:return_url',
  CODE_VERIFIER: 'auth:code_verifier',
  STATE: 'auth:state',
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

// Token timing constants
const TOKEN_REFRESH_BUFFER_MS = 5 * 60 * 1000;      // Refresh 5min before expiry
const TOKEN_EXPIRY_WARNING_MS = 10 * 60 * 1000;     // Warn 10min before expiry
const MIN_TOKEN_LIFETIME_MS = 60 * 1000;            // Don't refresh if <1min left
const CLOCK_SKEW_SEC = 60;                          // Clock skew leeway for exp

// Network constants
const MAX_RETRY_ATTEMPTS = 2;
const RETRY_BASE_DELAY_MS = 1000;
const NETWORK_TIMEOUT_MS = 10000;
const REVOKE_TIMEOUT_MS = 3000;

@Injectable({ providedIn: 'root' })
export class AuthService implements OnDestroy {
  private readonly api = environment.apiUrl;
  private readonly authUrl = environment.authUrl;
  private readonly destroy$ = new Subject<void>();

  // Cross-tab synchronization
  private readonly bc = this.initializeBroadcastChannel();

  // Single-flight refresh observable (multicast)
  private refresh$?: Observable<string>;

  // Token expiry timers
  private tokenExpiryTimer?: ReturnType<typeof setTimeout>;
  private tokenExpiryWarningTimer?: ReturnType<typeof setTimeout>;

  // Initialization tracking
  private initPromise?: Promise<void>;

  // Token expiry warning stream
  private readonly tokenExpiringSoon$ = new ReplaySubject<void>(1);

  // --- Reactive State (Angular Signals) ---
  private readonly authState = signal<AuthState>({ ...defaultAuthState });

  // --- Computed Properties ---
  readonly isAuthenticated = computed(() => this.authState().isAuthenticated);
  readonly currentUser = computed(() => this.authState().user);
  readonly roles = computed(() => this.authState().roles);
  readonly permissions = computed(() => this.authState().permissions);
  readonly isLoading = computed(() => this.authState().isLoading);
  readonly authError = computed(() => this.authState().error);
  readonly tokenExpiresAt = computed(() => this.authState().tokenExpiresAt);
  readonly refreshInProgress = computed(() => this.authState().refreshInProgress);

  // --- Observable Streams ---
  readonly authState$ = toObservable(this.authState).pipe(
    distinctUntilChanged((a, b) => this.authStatesEqual(a, b))
  );

  readonly isAuthenticated$ = this.authState$.pipe(
    map((s) => s.isAuthenticated),
    distinctUntilChanged()
  );

  readonly currentUser$ = this.authState$.pipe(
    map((s) => s.user),
    distinctUntilChanged()
  );

  readonly tokenExpiring$ = this.tokenExpiringSoon$.asObservable();

  // --- Dependencies ---
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  constructor() {
    this.restoreSessionFromStorage();
    this.setupBroadcastChannel();
    this.setupVisibilityListener();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.tokenExpiringSoon$.complete();
    this.bc.close();
  }

  // ====================
  // PUBLIC API
  // ====================

  /**
   * Initialize auth on app startup
   * Restores session from storage and loads user data if tokens valid
   */
  async init(): Promise<void> {
    if (this.initPromise) return this.initPromise;
    this.initPromise = this.performInit();
    return this.initPromise;
  }

  /**
   * Get current access token from session storage
   */
  getAccessToken(): string | null {
    return sessionStorage.getItem(SESSION_STORAGE_KEYS.ACCESS_TOKEN);
  }

  /**
   * Get valid access token, refreshing if necessary
   * Prevents race conditions with microtask queue guard
   */
  getValidAccessToken(): Observable<string> {
    const token = this.getAccessToken();

    // Token exists and is valid - return immediately
    if (this.isAuthenticated() && this.isTokenValid() && token) {
      return of(token);
    }

    // Refresh already in progress - return existing observable
    if (this.refresh$) return this.refresh$;

    // Start new refresh cycle with race condition guard
    if (!this.refresh$) {
      this.refresh$ = this.createRefreshTokenObservable();
      // Clear refresh$ after completion to allow new refresh cycles
      queueMicrotask(() => {
        this.refresh$?.subscribe();
      });
    }

    return this.refresh$;
  }

  /**
   * Initiate OAuth2 Authorization Code flow with PKCE
   * Redirects user to /connect/authorize on backend
   */
  async startLogin(returnUrl: string = this.router.url): Promise<void> {
    try {
      const sanitized = this.sanitizeReturnUrl(returnUrl);
      sessionStorage.setItem(SESSION_STORAGE_KEYS.RETURN_URL, sanitized);

      // Generate PKCE pair (code_verifier + code_challenge)
      const { code_verifier, code_challenge } = await createPkcePair();
      sessionStorage.setItem(SESSION_STORAGE_KEYS.CODE_VERIFIER, code_verifier);

      // Generate state for CSRF protection
      const state = generateRandomState();
      sessionStorage.setItem(SESSION_STORAGE_KEYS.STATE, state);

      // Build authorization URL
      const params = new URLSearchParams({
        client_id: environment.clientId,
        response_type: 'code',
        scope: 'openid profile email offline_access',
        redirect_uri: environment.redirectUri,
        code_challenge,
        code_challenge_method: 'S256',
        state,
      });

      // Redirect to backend authorize endpoint
      window.location.href = `${this.authUrl}/connect/authorize?${params.toString()}`;
    } catch (error) {
      this.failWithError(
        AuthErrorType.LOGIN_FAILED,
        'Failed to start login process',
        error,
        true
      );
    }
  }

  /**
   * Handle OAuth callback from backend
   * Validates state, exchanges code for tokens, loads user data
   */
  handleAuthCallback(code: string, state: string): Observable<void> {
    if (!code || !state) {
      return throwError(() =>
        this.createAuthError(AuthErrorType.LOGIN_FAILED, 'Missing code or state')
      );
    }

    // Validate CSRF state
    const storedState = sessionStorage.getItem(SESSION_STORAGE_KEYS.STATE);
    if (!storedState || storedState !== state) {
      sessionStorage.removeItem(SESSION_STORAGE_KEYS.STATE);
      return throwError(() =>
        this.createAuthError(AuthErrorType.INVALID_STATE, 'OAuth state mismatch')
      );
    }
    sessionStorage.removeItem(SESSION_STORAGE_KEYS.STATE);

    // Retrieve PKCE verifier
    const code_verifier = sessionStorage.getItem(SESSION_STORAGE_KEYS.CODE_VERIFIER);
    if (!code_verifier) {
      return throwError(() =>
        this.createAuthError(AuthErrorType.LOGIN_FAILED, 'PKCE verifier not found')
      );
    }
    sessionStorage.removeItem(SESSION_STORAGE_KEYS.CODE_VERIFIER);

    this.setAuthState({ isLoading: true });

    // Exchange authorization code for tokens
    return defer(() =>
      this.http.post<TokenResponse>(
        `${this.authUrl}/connect/token`,
        {
          grant_type: 'authorization_code',
          code,
          client_id: environment.clientId,
          redirect_uri: environment.redirectUri,
          code_verifier,
        },
        {
          context: new HttpContext()
            .set(SKIP_AUTH, true)
            .set(INCLUDE_CREDENTIALS, true),
        }
      )
    ).pipe(
      timeout(NETWORK_TIMEOUT_MS),
      tap((response) => this.storeTokens(response)),
      switchMap(() => this.loadUserDataAndPermissions()),
      catchError((error) => {
        this.failWithError(
          AuthErrorType.LOGIN_FAILED,
          'Failed to exchange authorization code',
          error,
          this.isRetryableError(error)
        );
        return throwError(() => error);
      }),
      finalize(() => this.setAuthState({ isLoading: false })),
      takeUntil(this.destroy$),
      map(() => void 0)
    );
  }

  /**
   * Logout: revoke refresh token and clear session
   * Skips if refresh is already in progress (may succeed)
   */
  logout(): Observable<void> {
    if (this.refreshInProgress()) {
      return of(void 0);
    }

    this.setAuthState({ isLoading: true });

    const refreshToken = sessionStorage.getItem(SESSION_STORAGE_KEYS.REFRESH_TOKEN);

    const revoke$ = refreshToken
      ? this.revokeToken(refreshToken).pipe(
          timeout(REVOKE_TIMEOUT_MS),
          catchError(() => of(void 0))
        )
      : of(void 0);

    return revoke$.pipe(
      tap(() => this.clearAuthSession()),
      finalize(() => {
        this.setAuthState({ isLoading: false });
        void this.router.navigate(['/signin']);
      }),
      takeUntil(this.destroy$)
    );
  }

  /**
   * Redirect to signin on 401 Unauthorized
   * Skips if refresh is in progress (let it recover)
   */
  handleUnauthorized(returnUrl: string = this.router.url): void {
    if (this.refreshInProgress()) return;

    const sanitized = this.sanitizeReturnUrl(returnUrl);
    sessionStorage.setItem(SESSION_STORAGE_KEYS.RETURN_URL, sanitized);
    this.clearAuthSession();

    queueMicrotask(() => {
      void this.router.navigate(['/signin'], {
        queryParams: {
          returnUrl: sessionStorage.getItem(SESSION_STORAGE_KEYS.RETURN_URL) ?? '/',
        },
      });
    });
  }

  // ====================
  // PRIVATE HELPERS
  // ====================

  private restoreSessionFromStorage(): void {
    const accessToken = sessionStorage.getItem(SESSION_STORAGE_KEYS.ACCESS_TOKEN);
    const expiresAt = sessionStorage.getItem(SESSION_STORAGE_KEYS.TOKEN_EXPIRES_AT);

    if (accessToken && expiresAt && parseInt(expiresAt, 10) > Date.now()) {
      this.setAuthState({
        isAuthenticated: true,
        tokenExpiresAt: parseInt(expiresAt, 10),
      });
    }
  }

  /**
   * Setup BroadcastChannel for cross-tab session sync
   * When user logs in/out in one tab, all tabs update
   * Safe for SSR environments
   */
  private initializeBroadcastChannel() {
    if (typeof BroadcastChannel === 'undefined') {
      // SSR or environment without BroadcastChannel support
      return {
        postMessage: () => {},
        close: () => {},
        onmessage: null as any,
      };
    }
    return new BroadcastChannel('auth');
  }

  /**
   * Setup BroadcastChannel listener for cross-tab sync
   */
  private setupBroadcastChannel(): void {
    if (!this.bc.onmessage) return; // Not initialized

    this.bc.onmessage = (event: MessageEvent) => {
      if (event.data?.type === 'auth:update') {
        this.restoreSessionFromStorage();
      } else if (event.data?.type === 'auth:clear') {
        this.clearAuthSession();
      }
    };
  }

  /**
   * Pause token timers when tab is hidden (battery saving)
   * Resume when tab is visible
   */
  private setupVisibilityListener(): void {
    document.addEventListener('visibilitychange', () => {
      if (!this.isAuthenticated()) return;

      if (document.hidden) {
        this.stopTokenTimers();
      } else {
        this.setupTokenExpiryTimer();
      }
    });
  }

  /**
   * Perform initialization: check existing session, load user data
   */
  private async performInit(): Promise<void> {
    try {
      this.setAuthState({ isLoading: true });

      const token = this.getAccessToken();
      const expAt = this.tokenExpiresAt();

      // No valid token - user not authenticated
      if (!token || !expAt || Date.now() >= expAt - TOKEN_REFRESH_BUFFER_MS) {
        this.clearAuthSession();
        return;
      }

      // Token exists and valid - load user data
      await firstValueFrom(this.loadUserDataAndPermissions());
      this.setupTokenExpiryTimer();
    } catch (error) {
      this.failWithError(
        AuthErrorType.INITIALIZATION_FAILED,
        'Failed to initialize auth',
        error,
        true
      );
      this.clearAuthSession();
    } finally {
      this.setAuthState({ isLoading: false });
    }
  }

  /**
   * Load user data from three endpoints in parallel
   * More efficient than sequential calls
   */
  private loadUserDataAndPermissions(): Observable<void> {
    const ctx = new HttpContext().set(INCLUDE_CREDENTIALS, true);

    const userInfo$ = this.http.get<UserInfoResponse>(
      `${this.authUrl}/connect/userinfo`,
      { context: ctx }
    );

    const perms$ = this.http.get<PermissionsResponse>(
      `${this.api}/users/permissions`,
      { context: ctx }
    );

    const profile$ = this.http.get<UserProfileResponse>(
      `${this.api}/users/profile`,
      { context: ctx }
    );

    return forkJoin({ userInfo: userInfo$, permResponse: perms$, profile: profile$ }).pipe(
      tap(({ userInfo, permResponse, profile }) => {
        const userProfile: UserProfile = {
          id: userInfo.sub,
          email: userInfo.email ?? profile.email ?? '',
          firstName: userInfo.given_name ?? profile.firstName ?? '',
          lastName: userInfo.family_name ?? profile.lastName ?? '',
          fullName: userInfo.name ?? profile.fullName ?? '',
          imageUrl: userInfo.picture ?? profile.imageUrl ?? '',
          roles: userInfo.role ?? [],
        };

        this.setAuthState({
          isAuthenticated: true,
          user: userProfile,
          permissions: permResponse.permissions ?? [],
          roles: userInfo.role ?? [],
          error: null,
        });
      }),
      catchError((error) => {
        this.failWithError(
          AuthErrorType.NETWORK_ERROR,
          'Failed to load user data',
          error,
          this.isRetryableError(error)
        );
        return throwError(() => error);
      }),
      map(() => void 0)
    );
  }

  /**
   * Store tokens with JWT exp parsing
   * Prefers JWT exp over expires_in for accuracy
   * Applies clock skew leeway (60 seconds)
   * Broadcasts to other tabs
   */
  private storeTokens(response: TokenResponse): void {
    const expFromJwt = getJwtExpSeconds(response.access_token);
    const expMs = expFromJwt
      ? (expFromJwt - CLOCK_SKEW_SEC) * 1000
      : Date.now() + response.expires_in * 1000;

    sessionStorage.setItem(SESSION_STORAGE_KEYS.ACCESS_TOKEN, response.access_token);
    if (response.refresh_token) {
      sessionStorage.setItem(SESSION_STORAGE_KEYS.REFRESH_TOKEN, response.refresh_token);
    }
    sessionStorage.setItem(SESSION_STORAGE_KEYS.TOKEN_EXPIRES_AT, expMs.toString());

    this.setAuthState({ tokenExpiresAt: expMs });
    this.setupTokenExpiryTimer();

    // Sync across tabs
    this.bc.postMessage({ type: 'auth:update' });
  }

  /**
   * Refresh token exchange with exponential backoff + jitter
   * Multicast via shareReplay to prevent duplicate HTTP requests
   */
  private createRefreshTokenObservable(): Observable<string> {
    this.setAuthState({ refreshInProgress: true });

    const refresh$ = defer(() => {
      const refreshToken = sessionStorage.getItem(SESSION_STORAGE_KEYS.REFRESH_TOKEN);
      if (!refreshToken) {
        throw this.createAuthError(
          AuthErrorType.REFRESH_FAILED,
          'No refresh token available'
        );
      }

      return this.http.post<TokenResponse>(
        `${this.authUrl}/connect/token`,
        {
          grant_type: 'refresh_token',
          refresh_token: refreshToken,
          client_id: environment.clientId,
        },
        {
          context: new HttpContext()
            .set(SKIP_AUTH, true)
            .set(INCLUDE_CREDENTIALS, true),
        }
      );
    }).pipe(
      timeout(NETWORK_TIMEOUT_MS),
      retry({
        count: MAX_RETRY_ATTEMPTS,
        delay: (_, i) => {
          const base = Math.pow(2, i) * RETRY_BASE_DELAY_MS; // 1s, 2s, 4s
          const jitter = Math.floor(Math.random() * 250); // +0-250ms
          return timer(base + jitter);
        },
      }),
      tap((response) => {
        this.storeTokens(response);
        // Reload user data after successful refresh
        this.loadUserDataAndPermissions().pipe(take(1)).subscribe();
      }),
      map(() => this.getAccessToken()!),
      catchError((error) => {
        const authError = this.createAuthError(
          AuthErrorType.REFRESH_FAILED,
          'Token refresh failed',
          error
        );
        this.setAuthError(authError);
        this.clearAuthSession();
        return throwError(() => authError);
      }),
      finalize(() => {
        this.refresh$ = undefined;
        this.setAuthState({ refreshInProgress: false });
      }),
      // Multicast: all subscribers share single HTTP request
      // refCount: true allows observable to teardown after completion
      shareReplay({ bufferSize: 1, refCount: true })
    );

    return refresh$;
  }

  /**
   * Revoke refresh token on backend
   */
  private revokeToken(token: string): Observable<void> {
    return this.http.post<void>(
      `${this.authUrl}/connect/revoke`,
      { token },
      {
        context: new HttpContext()
          .set(SKIP_AUTH, true)
          .set(INCLUDE_CREDENTIALS, true),
      }
    ).pipe(
      timeout(REVOKE_TIMEOUT_MS),
      catchError(() => of(void 0))
    );
  }

  /**
   * Setup timers for automatic token refresh
   * Warning timer: 10min before expiry (emits tokenExpiring$)
   * Refresh timer: 5min before expiry (calls getValidAccessToken)
   */
  private setupTokenExpiryTimer(): void {
    this.stopTokenTimers();

    const expiresAt = this.tokenExpiresAt();
    if (!expiresAt) return;

    const now = Date.now();
    const timeUntilRefresh = expiresAt - now - TOKEN_REFRESH_BUFFER_MS;
    const timeUntilWarning = expiresAt - now - TOKEN_EXPIRY_WARNING_MS;

    // Warn subscriber that token is about to expire
    if (timeUntilWarning > 0) {
      this.tokenExpiryWarningTimer = setTimeout(
        () => this.tokenExpiringSoon$.next(),
        timeUntilWarning
      );
    }

    // Automatically refresh token before expiry
    if (timeUntilRefresh > MIN_TOKEN_LIFETIME_MS) {
      this.tokenExpiryTimer = setTimeout(() => {
        if (this.isAuthenticated()) {
          this.getValidAccessToken().pipe(take(1)).subscribe();
        }
      }, timeUntilRefresh);
    }
  }

  private stopTokenTimers(): void {
    if (this.tokenExpiryTimer) clearTimeout(this.tokenExpiryTimer);
    if (this.tokenExpiryWarningTimer) clearTimeout(this.tokenExpiryWarningTimer);
    this.tokenExpiryTimer = this.tokenExpiryWarningTimer = undefined;
  }

  private clearAuthSession(): void {
    this.stopTokenTimers();
    Object.values(SESSION_STORAGE_KEYS).forEach((key) => {
      sessionStorage.removeItem(key);
    });
    this.authState.set({ ...defaultAuthState });
    this.refresh$ = undefined;
    this.initPromise = undefined;
    this.bc.postMessage({ type: 'auth:clear' });
  }

  private setAuthState(partial: Partial<AuthState>): void {
    this.authState.update((s) => ({ ...s, ...partial }));
  }

  private setAuthError(error: AuthError | null): void {
    this.authState.update((s) => ({ ...s, error, isLoading: false }));
  }

  private failWithError(
    type: AuthErrorType,
    message: string,
    originalError?: any,
    retryable = false
  ): void {
    const error = this.createAuthError(type, message, originalError, retryable);
    if (!environment.production) {
      console.error('[AuthService]', error);
    }
    this.setAuthError(error);
  }

  private createAuthError(
    type: AuthErrorType,
    message: string,
    originalError?: any,
    retryable = false
  ): AuthError {
    return { type, message, timestamp: Date.now(), originalError, retryable };
  }

  private isTokenValid(): boolean {
    const expiresAt = this.tokenExpiresAt();
    return !!expiresAt && Date.now() < expiresAt - TOKEN_REFRESH_BUFFER_MS;
  }

  private isRetryableError(error: any): boolean {
    if (!(error instanceof HttpErrorResponse)) return false;
    return (
      error.status === 0 || // Network error
      error.status === 408 || // Request timeout
      error.status === 429 || // Too many requests
      (error.status >= 500 && error.status < 600) // Server error
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

  private authStatesEqual(a: AuthState, b: AuthState): boolean {
    return (
      a.isAuthenticated === b.isAuthenticated &&
      a.isLoading === b.isLoading &&
      a.refreshInProgress === b.refreshInProgress &&
      a.tokenExpiresAt === b.tokenExpiresAt &&
      a.permissions.join(',') === b.permissions.join(',') &&
      a.roles.join(',') === b.roles.join(',')
    );
  }
}