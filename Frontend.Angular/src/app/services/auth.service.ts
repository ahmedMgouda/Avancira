import { HttpClient, HttpContext, HttpErrorResponse } from '@angular/common/http';
import { Injectable, OnDestroy } from '@angular/core';
import { Router, UrlTree } from '@angular/router';
import { BehaviorSubject, Observable, of, Subscription, throwError, timer } from 'rxjs';
import { catchError, finalize, map, retry, shareReplay, switchMap, take, tap } from 'rxjs/operators';
import { jwtDecode } from 'jwt-decode';

import { NotificationService } from './notification.service';
import { StorageService } from './storage.service';

import { environment } from '../environments/environment';
import { SKIP_AUTH } from '../interceptors/auth.interceptor';
import { UserProfile } from '../models/UserProfile';

interface TokenResponse { token: string; }

const CLOCK_SKEW_MS = 30_000;          // treat tokens expiring within 30s as expired

@Injectable({ providedIn: 'root' })
export class AuthService implements OnDestroy {
  private readonly PROFILE_KEY = 'user_profile';
  private readonly api = environment.apiUrl;

  private accessToken: string | null = null;
  private tokenExpiryMs: number | null = null;
  private refreshTimer?: Subscription;
  private refresh$?: Observable<string>; // de-dupes in-flight refreshes
  private refreshFailed = false; // tracks first refresh failure

  private profileSubject = new BehaviorSubject<UserProfile | null>(null);
  readonly profile$ = this.profileSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router,
    private storage: StorageService,
    private notifications: NotificationService
  ) {
    // UI continuity only (token never persisted)
    const cached = this.storage.getItem(this.PROFILE_KEY);
    if (cached) {
      try { this.profileSubject.next(JSON.parse(cached)); }
      catch { this.storage.removeItem(this.PROFILE_KEY); }
    }

    // Silent sign-in on app start if a refresh cookie exists
    this.refreshAccessToken().pipe(take(1)).subscribe({
      error: () => this.clearSession(false)
    });
  }

  // ---------- Public API

  isAuthenticated(): boolean {
    return !!(
      this.accessToken &&
      this.tokenExpiryMs !== null &&
      this.tokenExpiryMs > Date.now() + CLOCK_SKEW_MS
    );
  }

  getAccessToken(): string | null { return this.accessToken; }

  login(email: string, password: string): Observable<UserProfile> {
    return this.http.post<TokenResponse>(
      `${this.api}/auth/token`,
      { email, password },
      { withCredentials: true, context: new HttpContext().set(SKIP_AUTH, true) }
    ).pipe(
      tap(res => {
        if (!res?.token) throw new Error('No token returned');
        this.applyToken(res.token); // throws on invalid/expired token
      }),
      switchMap(() => this.loadPermissions()),
      tap(perms => this.patchProfile({ permissions: perms } as Partial<UserProfile>)),
      map(() => this.profileSubject.value as UserProfile),
      catchError(e => this.appError(e))
    );
  }

  logout(navigate = true): void {
    this.http.post(
      `${this.api}/auth/revoke`,
      {},
      { withCredentials: true }
    )
    .pipe(catchError(() => of(null)))
    .subscribe(() => {
      this.clearSession();
      if (navigate) {
        this.router.navigateByUrl(this.redirectToSignIn());
      }
    });
  }

  redirectToSignIn(returnUrl: string = this.router.url): UrlTree {
    return this.router.createUrlTree(['/signin'], { queryParams: { returnUrl } });
  }

  /** Obtain a valid access token, refreshing silently if needed. */
  getValidAccessToken(): Observable<string> {
    return this.isAuthenticated()
      ? of(this.accessToken as string)
      : this.refreshAccessToken();
  }

  getProfile(): UserProfile | null { return this.profileSubject.value; }
  hasRole(r: string): boolean { return this.any((this.profileSubject.value as any)?.roles, r); }
  hasAnyRole(...r: string[]): boolean { return this.any((this.profileSubject.value as any)?.roles, ...r); }
  hasPermission(p: string): boolean { return this.any((this.profileSubject.value as any)?.permissions, p); }
  hasAnyPermission(...p: string[]): boolean { return this.any((this.profileSubject.value as any)?.permissions, ...p); }

  // ---------- Refresh (single, de-duped path using retry({...}))

  refreshAccessToken(): Observable<string> {
    if (this.isAuthenticated()) return of(this.accessToken as string);
    if (this.refreshFailed) return throwError(() => new Error('REFRESH_FAILED'));
    if (this.refresh$) return this.refresh$;

    this.refresh$ = this.http.post<TokenResponse>(
      `${this.api}/auth/refresh`,
      {},
      { withCredentials: true, context: new HttpContext().set(SKIP_AUTH, true) }
    ).pipe(
      retry({
        count: 2, // total 3 tries (initial + 2 retries)
        delay: (err: any, i: number) => {
          const s = (err as HttpErrorResponse)?.status;
          const transient = s === 0 || s === 502 || s === 503 || s === 504;
          if (!transient) throw err;
          return timer(Math.pow(2, i) * 1000); // 1s, 2s
        },
        resetOnSuccess: true
      }),
      map(res => {
        if (!res?.token) throw new Error('No token on refresh');
        this.applyToken(res.token); // throws on invalid/expired token
        return res.token;
      }),
      shareReplay({ bufferSize: 1, refCount: true }),
      catchError(err => {
        this.refreshFailed = true;
        return throwError(() => err);
      }),
      finalize(() => { this.refresh$ = undefined; })
    );

    return this.refresh$;
  }

  // ---------- Internals

  private applyToken(token: string): void {
    const mapped = this.decode(token);
    if (!mapped) {
      // malformed/missing exp/expired token → treat as unauthorized
      throw new Error('INVALID_TOKEN');
    }
    this.refreshFailed = false;
    this.accessToken = token;
    const exp = (mapped as any).exp as number | undefined;
    this.tokenExpiryMs = exp ? exp * 1000 : null;
    this.patchProfile(mapped);

    if (exp) this.scheduleEarlyRefresh(exp);
  }

  /** Refresh ~2 minutes before expiry (with a small jitter). */
  private scheduleEarlyRefresh(expUnixSec: number): void {
    this.refreshTimer?.unsubscribe();
    const early = 120_000; // 2 min
    const jitter = Math.floor(Math.random() * 20_000); // +0..20s
    const ms = Math.max(0, expUnixSec * 1000 - Date.now() - early - jitter);
    this.refreshTimer = timer(ms).pipe(
      switchMap(() => this.refreshAccessToken()),
      take(1)
    ).subscribe({
      error: () => this.logout() // if refresh fails right before expiry → sign out
    });
  }

  /** Map JWT → your UserProfile; null if invalid/expired. */
  private decode(token: string): Partial<UserProfile> | null {
    try {
      const d: any = jwtDecode(token);

      const expSec = Number(d?.exp ?? 0);
      const expMs = expSec * 1000;
      if (!Number.isFinite(expSec) || expMs <= Date.now() + CLOCK_SKEW_MS) {
        return null; // missing/invalid/expired exp → unauthorized
      }


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
    } catch {
      return null;
    }
  }

  private loadPermissions(): Observable<string[]> {
    return this.http.get<string[]>(`${this.api}/users/permissions`).pipe(catchError(() => of([])));
  }

  private patchProfile(patch: Partial<UserProfile>): void {
    const merged = { ...(this.profileSubject.value ?? {}), ...patch } as UserProfile;
    this.profileSubject.next(merged);
    try { this.storage.setItem(this.PROFILE_KEY, JSON.stringify(merged)); } catch {}
  }

  private any(list: string[] | undefined, ...items: string[]) {
    if (!list?.length || !items.length) return false;
    const s = new Set(list); return items.some(i => s.has(i));
  }
  private arr(v: any): string[] { return Array.isArray(v) ? v : v ? [v] : []; }

  private clearSession(navigate = false): void {
    this.accessToken = null;
    this.tokenExpiryMs = null;
    this.profileSubject.next(null);

    try {
      this.storage.removeItem(this.PROFILE_KEY);
    } catch {}

    this.refreshTimer?.unsubscribe(); this.refreshTimer = undefined;
    this.refreshFailed = true;
    this.notifications.stopConnection();
    if (navigate) this.router.navigateByUrl(this.redirectToSignIn());
  }

  private appError(err: HttpErrorResponse | any) {
    return throwError(() => new Error(err?.error?.message || err?.message || 'Authentication error'));
  }

  ngOnDestroy(): void {
    this.refreshTimer?.unsubscribe();
    this.notifications.stopConnection();
  }
}
