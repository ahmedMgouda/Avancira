import { HttpClient, HttpContext, HttpErrorResponse } from '@angular/common/http';
import { Injectable, OnDestroy } from '@angular/core';
import { Router, UrlTree } from '@angular/router';
import { BehaviorSubject, Observable, of, Subscription, throwError, timer } from 'rxjs';
import { catchError, finalize, map, retry, shareReplay, switchMap, take, tap } from 'rxjs/operators';
import { jwtDecode } from 'jwt-decode';

import { environment } from '../environments/environment';
import { INCLUDE_CREDENTIALS, REQUIRES_AUTH, SKIP_AUTH } from '../interceptors/auth.interceptor';
import { RegisterUserResponseDto } from '../models/register-user-response';
import { UserProfile } from '../models/UserProfile';

interface TokenResponse { token: string; }

// Light, internal constants (you can move to env later if you want)
const CLOCK_SKEW_MS    = 30_000;
const EARLY_REFRESH_MS = 120_000;
const JITTER_MAX_MS    = 20_000;

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

  // In-memory token (not persisted)
  private accessToken: string | null = null;
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

  login(email: string, password: string, rememberMe = false): Observable<UserProfile> {
    return this.http.post<TokenResponse>(
      `${this.api}/auth/token`,
      { email, password, rememberMe },
      { context: new HttpContext().set(SKIP_AUTH, true).set(INCLUDE_CREDENTIALS, true) }
    ).pipe(
      tap(res => { if (!res?.token) throw new Error('NO_TOKEN'); this.applyToken(res.token); }),
      switchMap(() => this.http.get<string[]>(`${this.api}/users/permissions`).pipe(catchError(() => of([])))),
      tap(perms => this.patchProfile({ permissions: perms } as Partial<UserProfile>)),
      map(() => { this.setState(AuthStateKind.Authenticated); return this.profileSubject.value as UserProfile; })
    );
  }

  register(
    firstName: string,
    lastName: string,
    userName: string,
    email: string,
    password: string,
    confirmPassword: string,
    phoneNumber?: string,
    timeZoneId?: string,
    referralToken?: string,
    acceptTerms?: boolean,
  ): Observable<RegisterUserResponseDto> {
    return this.http.post<RegisterUserResponseDto>(
      `${this.api}/users/register`,
      {
        firstName,
        lastName,
        email,
        userName,
        password,
        confirmPassword,
        phoneNumber,
        timeZoneId,
        referralToken,
        acceptTerms,
      },
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
    // Race-safety: if not forcing and token became valid in the meantime, bail out
    if (!force && this.isAuthenticated()) return of(this.accessToken as string);
    if (this.authLocked) return throwError(() => new Error('AUTH_LOCKED'));
    if (this.refresh$) return this.refresh$;

    this.setState(AuthStateKind.Refreshing);

    this.refresh$ = this.http.post<TokenResponse>(
      `${this.api}/auth/refresh`, {},
      { context: new HttpContext().set(SKIP_AUTH, true).set(INCLUDE_CREDENTIALS, true) }
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
        if (!res?.token) throw new Error('NO_TOKEN_REFRESH');
        this.applyToken(res.token);
        this.setState(AuthStateKind.Authenticated);
        return res.token;
      }),
      shareReplay({ bufferSize: 1, refCount: true }),
      catchError(err => { this.onRefreshFailed(); return throwError(() => err); }),
      finalize(() => { this.refresh$ = undefined; })
    );

    return this.refresh$;
  }

  /* ---------------- Internals ---------------- */

  private applyToken(token: string): void {
    const mapped = this.decode(token);
    if (!mapped) throw new Error('INVALID_TOKEN');

    this.authLocked = false;
    this.accessToken = token;

    const exp = (mapped as any).exp as number | undefined;
    this.tokenExpiryMs = exp ? exp * 1000 : null;

    this.patchProfile(mapped);
    if (exp) this.scheduleEarlyRefresh(exp);
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

  ngOnDestroy(): void { this.refreshTimer?.unsubscribe(); }
}
