import { HttpClient, HttpContext, HttpErrorResponse, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, OnDestroy } from '@angular/core';
import { Router, UrlTree } from '@angular/router';
import { BehaviorSubject, Observable, of, Subscription, throwError, timer } from 'rxjs';
import { catchError, finalize, map, retry, shareReplay, switchMap, take, tap } from 'rxjs/operators';
import { jwtDecode } from 'jwt-decode';

import { environment } from '../environments/environment';
import { INCLUDE_CREDENTIALS, REQUIRES_AUTH, SKIP_AUTH } from '../interceptors/auth.interceptor';
import { RegisterUserRequest } from '../models/register-user-request';
import { RegisterUserResponseDto } from '../models/register-user-response';
import { SocialProvider } from '../models/social-provider';
import { UserProfile } from '../models/UserProfile';

interface TokenResponse {
  access_token: string;
  refresh_token?: string;
}

// Light, internal constants (you can move to env later if you want)
const CLOCK_SKEW_MS    = 30_000;
const EARLY_REFRESH_MS = 120_000;
const JITTER_MAX_MS    = 20_000;
const PKCE_VERIFIER_KEY = 'pkce_verifier';

/** Internal auth state (strongly typed, no strings) */
export enum AuthStateKind {
  Unauthenticated,
  Refreshing,
  Authenticated,
  Expired
}

@Injectable({ providedIn: 'root' })
export class AuthService implements OnDestroy {
  private readonly PROFILE_KEY = 'user_profile';
  private readonly api = environment.apiUrl;
  private readonly base = environment.baseApiUrl;

  // In-memory tokens (not persisted)
  private accessToken: string | null = null;
  private refreshToken: string | null = null;
  private tokenExpiryMs: number | null = null;

  /** Early refresh timer */
  private refreshTimer?: Subscription;

  /** Single-flight refresh queue */
  private refresh$?: Observable<string>;

  /** Block refresh attempts after a hard fail until next login */
  private authLocked = false;

  /** Published profile (UI continuity only; token is memory-only) */
  private profileSubject = new BehaviorSubject<UserProfile | null>(null);
  readonly profile$ = this.profileSubject.asObservable();

  /** Published auth state so any component can subscribe */
  private stateSubject = new BehaviorSubject<AuthStateKind>(AuthStateKind.Unauthenticated);
  readonly authState$  = this.stateSubject.asObservable();

  /** A handy selector many UIs want */
  readonly isAuthenticated$ = this.authState$.pipe(
    map(s => s === AuthStateKind.Authenticated),
    shareReplay({ bufferSize: 1, refCount: true })
  );

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    // Restore profile only (not token)
    const cached = localStorage.getItem(this.PROFILE_KEY);
    if (cached) {
      try { this.profileSubject.next(JSON.parse(cached)); }
      catch { localStorage.removeItem(this.PROFILE_KEY); }
    }
  }

  /* ---------------- Public API ---------------- */

  /** Synchronous check for guards/interceptors */
  isAuthenticated(): boolean {
    return !!(this.accessToken && this.tokenExpiryMs && this.tokenExpiryMs > Date.now() + CLOCK_SKEW_MS);
  }

  /** Guards can wait only if a refresh is already running (no network here). */
  waitForRefresh(): Observable<unknown> { return this.refresh$ ?? of(null); }

  getAccessToken(): string | null { return this.accessToken; }

  /** Single entry: returns a valid token or joins the refresh queue (no navigation here). */
  getValidAccessToken(): Observable<string> {
    if (this.isAuthenticated()) return of(this.accessToken as string);
    return this.refreshAccessToken(); // normal path (not forced)
  }

  async startLogin(returnUrl = this.router.url): Promise<void> {
    const verifier = this.generateVerifier();
    sessionStorage.setItem(PKCE_VERIFIER_KEY, verifier);
    const challenge = await this.generateChallenge(verifier);
    const params = new URLSearchParams({
      response_type: 'code',
      client_id: 'frontend',
      scope: 'api offline_access',
      redirect_uri: `${environment.frontendUrl}/signin-callback`,
      code_challenge: challenge,
      code_challenge_method: 'S256',
      state: returnUrl
    });
    window.location.href = `${this.base}/connect/authorize?${params.toString()}`;
  }

  completeLogin(code: string): Observable<UserProfile> {
    const verifier = sessionStorage.getItem(PKCE_VERIFIER_KEY);
    if (!verifier) return throwError(() => new Error('MISSING_VERIFIER'));
    const body = new HttpParams()
      .set('grant_type', 'authorization_code')
      .set('code', code)
      .set('redirect_uri', `${environment.frontendUrl}/signin-callback`)
      .set('code_verifier', verifier);
    return this.handleCodeExchange(
      this.http.post<TokenResponse>(
        `${this.base}/connect/token`,
        body.toString(),
        {
          headers: new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' }),
          context: new HttpContext().set(SKIP_AUTH, true).set(INCLUDE_CREDENTIALS, true)
        }
      )
    );
  }

  externalLogin(provider: SocialProvider, token: string): Observable<UserProfile> {
    const csrf = this.getCsrfToken();
    let headers = new HttpHeaders();
    if (csrf) {
      headers = headers.set('X-CSRF-TOKEN', csrf);
    }
    return this.handleCodeExchange(
      this.http
        .post<{ token: string }>(
          `${this.api}/auth/external-login`,
          { provider, token },
          {
            context: new HttpContext().set(SKIP_AUTH, true).set(INCLUDE_CREDENTIALS, true),
            headers
          }
        )
        .pipe(map(res => ({ access_token: res.token } as TokenResponse)))
    );
  }

  register(data: RegisterUserRequest): Observable<RegisterUserResponseDto> {
    return this.http.post<RegisterUserResponseDto>(
      `${this.api}/users/register`,
      data,
      {
        context: new HttpContext().set(SKIP_AUTH, true).set(INCLUDE_CREDENTIALS, true),
      }
    );
  }

  /** User-initiated logout (this one may navigate) */
  logout(navigate = true): void {
    this.http.post(
      `${this.api}/auth/revoke`,
      {},
      { context: new HttpContext().set(REQUIRES_AUTH, true).set(INCLUDE_CREDENTIALS, true) }
    )
    .pipe(catchError(() => of(null)))
    .subscribe(() => {
      this.clearSession(AuthStateKind.Unauthenticated);
      if (navigate) this.router.navigateByUrl(this.redirectToSignIn());
    });
  }

  /** Helper so the guard can build a consistent redirect */
  redirectToSignIn(returnUrl: string = this.router.url): UrlTree {
    return this.router.createUrlTree(['/signin'], { queryParams: { returnUrl } });
  }

  /* ---------------- Refresh (single-flight; no navigation on failure) ---------------- */

  private refreshAccessToken(force = false): Observable<string> {
    if (!force && this.isAuthenticated()) return of(this.accessToken as string);
    if (this.authLocked) return throwError(() => new Error('AUTH_LOCKED'));
    if (!this.refreshToken) return throwError(() => new Error('NO_REFRESH_TOKEN'));
    if (this.refresh$) return this.refresh$;

    this.setState(AuthStateKind.Refreshing);

    const body = new HttpParams()
      .set('grant_type', 'refresh_token')
      .set('refresh_token', this.refreshToken);

    this.refresh$ = this.http.post<TokenResponse>(
      `${this.base}/connect/token`,
      body.toString(),
      {
        headers: new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' }),
        context: new HttpContext().set(SKIP_AUTH, true).set(INCLUDE_CREDENTIALS, true)
      }
    ).pipe(
      retry({
        count: 2,
        delay: (err: any, i: number) => {
          const s = (err as HttpErrorResponse)?.status;
          const transient = s === 0 || s === 502 || s === 503 || s === 504;
          if (!transient) throw err;
          return timer(Math.pow(2, i) * 1000); // 1s, 2s
        },
        resetOnSuccess: true
      }),
      map(res => {
        if (!res?.access_token) throw new Error('NO_TOKEN_REFRESH');
        this.applyToken(res.access_token, res.refresh_token);
        this.setState(AuthStateKind.Authenticated);
        return res.access_token;
      }),
      shareReplay({ bufferSize: 1, refCount: true }),
      catchError(err => { this.onRefreshFailed(); return throwError(() => err); }),
      finalize(() => { this.refresh$ = undefined; })
    );

    return this.refresh$;
  }

  /* ---------------- Internals ---------------- */

  private handleCodeExchange(source: Observable<TokenResponse>): Observable<UserProfile> {
    return source.pipe(
      tap(res => { if (!res?.access_token) throw new Error('NO_TOKEN'); this.applyToken(res.access_token, res.refresh_token); }),
      switchMap(() => this.http.get<string[]>(`${this.api}/users/permissions`).pipe(catchError(() => of([])))),
      tap(perms => this.patchProfile({ permissions: perms } as Partial<UserProfile>)),
      map(() => { this.setState(AuthStateKind.Authenticated); return this.profileSubject.value as UserProfile; })
    );
  }

  private applyToken(token: string, refresh?: string): void {
    const mapped = this.decode(token);
    if (!mapped) throw new Error('INVALID_TOKEN');

    this.authLocked = false;
    this.accessToken = token;
    if (refresh) this.refreshToken = refresh;

    const exp = (mapped as any).exp as number | undefined;
    this.tokenExpiryMs = exp ? exp * 1000 : null;

    this.patchProfile(mapped);
    if (exp) this.scheduleEarlyRefresh(exp);
  }

  private getCsrfToken(): string | null {
    const match = document.cookie.match(/(^|;\s*)CSRF-TOKEN=([^;]+)/);
    return match ? decodeURIComponent(match[2]) : null;
  }

  private scheduleEarlyRefresh(expUnixSec: number): void {
    this.refreshTimer?.unsubscribe();
    const jitter = Math.floor(Math.random() * JITTER_MAX_MS);
    const ms = Math.max(0, expUnixSec * 1000 - Date.now() - EARLY_REFRESH_MS - jitter);
    this.refreshTimer = timer(ms).pipe(
      switchMap(() => this.refreshAccessToken(true)), // force proactive refresh
      take(1)
    ).subscribe();
  }

  private decode(token: string): Partial<UserProfile> | null {
    try {
      const d: any = jwtDecode(token);
      const expSec = Number(d?.exp ?? 0);
      const expMs  = expSec * 1000;
      if (!Number.isFinite(expSec) || expMs <= Date.now() + CLOCK_SKEW_MS) return null;

      const profile: Partial<UserProfile> = {
        id: d.sub ?? '',
        email: d.email ?? '',
        firstName: d.given_name || '',
        lastName: d.family_name || '',
        fullName: d.fullName || `${d.given_name || ''} ${d.family_name || ''}`.trim(),
        roles: this.arr(d.role),
        permissions: this.arr(d.permissions),
        exp: expSec,
        timeZoneId: d.timeZoneId,
        ipAddress: d.ipAddress,
        imageUrl: d.image_url
      };
      return profile;
    } catch { return null; }
  }

  private patchProfile(patch: Partial<UserProfile>): void {
    const merged = { ...(this.profileSubject.value ?? {}), ...patch } as UserProfile;
    this.profileSubject.next(merged);
    try { localStorage.setItem(this.PROFILE_KEY, JSON.stringify(merged)); } catch {}
  }

  private clearSession(next: AuthStateKind): void {
    this.accessToken = null;
    this.refreshToken = null;
    this.tokenExpiryMs = null;
    this.profileSubject.next(null);
    try { localStorage.removeItem(this.PROFILE_KEY); } catch {}
    this.refreshTimer?.unsubscribe(); this.refreshTimer = undefined;
    this.authLocked = true;
    this.setState(next);
  }

  private onRefreshFailed(): void {
    // No navigation — guard will redirect when user hits a protected route
    this.clearSession(AuthStateKind.Expired);
  }

  private setState(s: AuthStateKind) { this.stateSubject.next(s); }

  private arr(v: any): string[] { return Array.isArray(v) ? v : v ? [v] : []; }

  private generateVerifier(): string {
    const data = new Uint8Array(32);
    crypto.getRandomValues(data);
    return this.base64UrlEncode(data);
  }

  private async generateChallenge(verifier: string): Promise<string> {
    const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier));
    return this.base64UrlEncode(digest);
    }

  private base64UrlEncode(data: ArrayBuffer | Uint8Array): string {
    const arr = data instanceof Uint8Array ? data : new Uint8Array(data);
    return btoa(String.fromCharCode(...arr))
      .replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  }

  ngOnDestroy(): void { this.refreshTimer?.unsubscribe(); }
}
