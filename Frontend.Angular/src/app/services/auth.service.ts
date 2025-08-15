import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import {
  BehaviorSubject,
  Observable,
  of,
  Subscription,
  throwError,
  timer} from 'rxjs';
import {
  catchError,
  filter,
  map,
  switchMap,
  take,
  tap
} from 'rxjs/operators';
import { jwtDecode } from 'jwt-decode';

import { NotificationService } from './notification.service';

import { environment } from '../environments/environment';
import { UserProfile } from '../models/UserProfile';

export interface TokenResponse {
  token: string;
  refreshToken?: string;
  refreshTokenExpiryTime?: string; // ISO string (UTC)
}

@Injectable({ providedIn: 'root' })
export class AuthService implements OnDestroy {
  private readonly PROFILE_KEY = 'user_profile';
  private readonly ACCESS_TOKEN_KEY = 'access_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token';
  private readonly REFRESH_EXPIRY_KEY = 'refresh_token_expiry';
  private readonly apiBase = environment.apiUrl;

  private accessToken: string | null = null;
  private refreshTimerSub: Subscription | null = null;

  private profileSubject = new BehaviorSubject<UserProfile | null>(null);
  profile$ = this.profileSubject.asObservable();

  private isRefreshing = false;
  private refreshTokenSubject = new BehaviorSubject<string | null>(null);

  private boundStorageListener: ((e: StorageEvent) => void) | null = null;

  constructor(
    private http: HttpClient,
    private router: Router,
    private notificationService: NotificationService
  ) {
    this.initStorageListener();
    this.restoreProfile();
  }

  beginRefresh(): void {
    this.isRefreshing = true;
    this.refreshTokenSubject.next(null);
  }

  endRefresh(token: string): void {
    this.isRefreshing = false;
    this.refreshTokenSubject.next(token);
  }

  refreshFailed(error?: any): void {
    this.isRefreshing = false;
    this.refreshTokenSubject.error(error ?? new Error('Token refresh failed'));
    // Recreate subject for future waiters
    this.refreshTokenSubject = new BehaviorSubject<string | null>(null);
  }

  waitForRefresh(): Observable<string> {
    return this.refreshTokenSubject.pipe(
      filter((token): token is string => token !== null),
      take(1)
    );
  }

  get refreshing(): boolean {
    return this.isRefreshing;
  }

  login(email: string, password: string): Observable<UserProfile> {
    return this.http
      .post<TokenResponse>(`${this.apiBase}/auth/token`, { email, password })
      .pipe(
        switchMap((res) => {
          if (!res?.token) {
            return throwError(() => new Error('No token returned from server'));
          }

          // Store tokens + profile + schedule auto refresh
          this.applyTokens(res);

          // Load permissions and merge into profile
          return this.loadPermissions().pipe(
            map((perms) => {
              this.updateProfile({ permissions: perms });
              return this.profileSubject.value!;
            })
          );
        }),
        catchError((err) => this.handleError(err))
      );
  }

  logout(): void {
    if (!this.accessToken && !this.profileSubject.value) return;
    this.clearSession();
    this.router.navigate(['/signin'], { queryParams: { returnUrl: this.router.url } });
  }

  isAuthenticated(): boolean {
    const token = this.accessToken || this.getStorage(this.ACCESS_TOKEN_KEY);
    const refreshToken = this.getStorage(this.REFRESH_TOKEN_KEY);
    const refreshExpiry = this.getStorage(this.REFRESH_EXPIRY_KEY);

    const accessValid = !!(token && this.tokenExpiryMs(token) > Date.now());
    const refreshValid = !!(refreshToken && refreshExpiry && Date.parse(refreshExpiry) > Date.now());

    // Only clear session if BOTH are invalid and we had some state
    if (!accessValid && !refreshValid && (token || refreshToken || this.profileSubject.value)) {
      this.clearSession();
    }

    return accessValid || refreshValid;
  }

  getAccessToken(): string | null {
    return this.accessToken || this.getStorage(this.ACCESS_TOKEN_KEY);
  }

  refreshToken(): Observable<TokenResponse> {
    const token = this.accessToken || this.getStorage(this.ACCESS_TOKEN_KEY);
    const refreshToken = this.getStorage(this.REFRESH_TOKEN_KEY);

    if (!token || !refreshToken) {
      this.logout();
      return throwError(() => new Error('Missing token or refresh token'));
    }

    return this.http
      .post<TokenResponse>(`${this.apiBase}/auth/refresh`, {
        Token: token,
        RefreshToken: refreshToken
      })
      .pipe(
        tap((res) => {
          if (res?.token) {
            this.applyTokens(res);
          }
        }),
        catchError((err) => {
          this.logout();
          return this.handleError(err);
        })
      );
  }

  getProfile(): UserProfile | null {
    return this.profileSubject.value;
  }

  hasRole(role: string): boolean {
    return this.hasAny(this.profileSubject.value?.roles, role);
  }

  hasAnyRole(...roles: string[]): boolean {
    return this.hasAny(this.profileSubject.value?.roles, ...roles);
  }

  hasPermission(permission: string): boolean {
    return this.hasAny(this.profileSubject.value?.permissions, permission);
  }

  hasAnyPermission(...perms: string[]): boolean {
    return this.hasAny(this.profileSubject.value?.permissions, ...perms);
  }

  // ===== Private helpers =====

  private applyTokens(res: TokenResponse): void {
    // Persist tokens
    this.setTokensToStorage(res);

    // Keep in-memory access token for perf
    this.accessToken = res.token;

    // Decode and update profile (idempotent)
    const profile = this.decodeToken(res.token);
    this.updateProfile(profile);

    // Schedule auto refresh a little before expiry
    if (profile.exp) this.scheduleRefresh(profile.exp);
  }

  private setTokensToStorage({ token, refreshToken, refreshTokenExpiryTime }: TokenResponse): void {
    if (token) this.setStorage(this.ACCESS_TOKEN_KEY, token);
    if (refreshToken) this.setStorage(this.REFRESH_TOKEN_KEY, refreshToken);
    if (refreshTokenExpiryTime) this.setStorage(this.REFRESH_EXPIRY_KEY, refreshTokenExpiryTime);
  }

  private restoreProfile(): void {
    const storedAccess = this.getStorage(this.ACCESS_TOKEN_KEY);
    const storedRefresh = this.getStorage(this.REFRESH_TOKEN_KEY);
    const storedRefreshExp = this.getStorage(this.REFRESH_EXPIRY_KEY);

    const now = Date.now();
    const accessExpired = !storedAccess || this.tokenExpiryMs(storedAccess) <= now;
    const refreshExpired =
      !storedRefresh || !storedRefreshExp || Date.parse(storedRefreshExp) <= now;

    if (refreshExpired) {
      // No way to recover → clear
      this.clearSession();
      return;
    }

    // Keep any cached profile for UI continuity
    const cached = this.getStorage(this.PROFILE_KEY);
    if (cached) {
      try {
        this.profileSubject.next(JSON.parse(cached));
      } catch {
        localStorage.removeItem(this.PROFILE_KEY);
      }
    }

    if (!accessExpired && storedAccess) {
      // Access still valid
      this.accessToken = storedAccess;
      const { exp } = this.decodeToken(storedAccess);
      if (exp) this.scheduleRefresh(exp);
    } else {
      // Access missing/expired but refresh is valid → refresh immediately
      this.beginRefresh();
      this.refreshToken()
        .pipe(take(1))
        .subscribe({
          next: (r) => this.endRefresh(r.token),
          error: (e) => this.refreshFailed(e)
        });
    }
  }

  private updateProfile(patch: Partial<UserProfile>): void {
    const updated = { ...this.profileSubject.value, ...patch } as UserProfile;
    this.profileSubject.next(updated);
    this.setStorage(this.PROFILE_KEY, JSON.stringify(updated));
  }

  /**
   * Schedule a refresh ~2 minutes before exp.
   * Important: keep the timer subscription separate from the HTTP subscription
   * to avoid canceling in-flight refresh requests.
   */
  private scheduleRefresh(expSeconds: number): void {
    this.cancelRefresh();

    const msUntilExp = expSeconds * 1000 - Date.now();
    const refreshInMs = Math.max(0, msUntilExp - 2 * 60_000);

    this.refreshTimerSub = timer(refreshInMs).subscribe(() => {
      this.beginRefresh();
      this.refreshToken()
        .pipe(take(1))
        .subscribe({
          next: (r) => this.endRefresh(r.token),
          error: (e) => this.refreshFailed(e)
        });
    });
  }

  private cancelRefresh(): void {
    this.refreshTimerSub?.unsubscribe();
    this.refreshTimerSub = null;
  }

  private decodeToken(token: string): UserProfile {
    const decoded: any = jwtDecode(token);
    // Persist device id if present in the token
    if (decoded?.device_id) {
      localStorage.setItem('deviceId', decoded.device_id);
    }

    return {
      id: decoded.sub,
      email: decoded.email,
      firstName: decoded.given_name || '',
      lastName: decoded.family_name || '',
      fullName:
        decoded.fullName ||
        `${decoded.given_name || ''} ${decoded.family_name || ''}`.trim(),
      timeZoneId: decoded.timeZoneId,
      ipAddress: decoded.ipAddress,
      imageUrl: decoded.image_url,
      deviceId: decoded.device_id,
      roles: this.toArray(decoded.role),
      permissions: this.toArray(decoded.permissions),
      exp: decoded.exp
    };
  }

  private tokenExpiryMs(token: string): number {
    try {
      const exp = (jwtDecode<any>(token).exp ?? 0) * 1000;
      return Number.isFinite(exp) ? exp : 0;
    } catch {
      return 0;
    }
  }

  private loadPermissions(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiBase}/users/permissions`).pipe(
      catchError(() => of([]))
    );
  }

  private hasAny(list: string[] | undefined, ...items: string[]): boolean {
    return !!list?.some((v) => items.includes(v));
  }

  private toArray(val: any): string[] {
    return Array.isArray(val) ? val : val ? [val] : [];
    }

  private getStorage(key: string): string | null {
    return localStorage.getItem(key);
  }

  private setStorage(key: string, value: string): void {
    localStorage.setItem(key, value);
  }

  private clearSession(): void {
    this.accessToken = null;
    this.profileSubject.next(null);
    [
      this.PROFILE_KEY,
      this.REFRESH_TOKEN_KEY,
      this.REFRESH_EXPIRY_KEY,
      this.ACCESS_TOKEN_KEY
    ].forEach((k) => localStorage.removeItem(k));

    this.cancelRefresh();
    this.notificationService.stopConnection();
  }

  private handleError(err: HttpErrorResponse | any) {
    console.error('AuthService error', err);
    return throwError(
      () => new Error(err?.error?.message || err?.message || 'Authentication error')
    );
  }

  // Cross-tab sync: if another tab logs in/out, react here
  private initStorageListener(): void {
    if (typeof window === 'undefined') return;
    this.boundStorageListener = (e: StorageEvent) => {
      if (
        e.key === this.ACCESS_TOKEN_KEY ||
        e.key === this.REFRESH_TOKEN_KEY ||
        e.key === this.REFRESH_EXPIRY_KEY ||
        e.key === this.PROFILE_KEY
      ) {
        // Re-evaluate current state
        this.restoreProfile();
      }
    };
    window.addEventListener('storage', this.boundStorageListener);
  }

  ngOnDestroy(): void {
    this.cancelRefresh();
    this.notificationService.stopConnection();
    if (this.boundStorageListener) {
      window.removeEventListener('storage', this.boundStorageListener);
      this.boundStorageListener = null;
    }
  }
}
