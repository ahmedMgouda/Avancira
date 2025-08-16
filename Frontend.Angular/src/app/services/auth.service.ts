import { HttpClient, HttpContext, HttpErrorResponse } from '@angular/common/http';
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
  finalize,
  map,
  switchMap,
  take,
  tap
} from 'rxjs/operators';
import { jwtDecode } from 'jwt-decode';

import { NotificationService } from './notification.service';
import { StorageService } from './storage.service';

import { environment } from '../environments/environment';
import { SKIP_AUTH } from '../interceptors/auth.interceptor';
import { UserProfile } from '../models/UserProfile';

export interface TokenResponse {
  token: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService implements OnDestroy {
  private readonly PROFILE_KEY = 'user_profile';
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
    private notificationService: NotificationService,
    private storage: StorageService
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
      .post<TokenResponse>(
        `${this.apiBase}/auth/token`,
        { email, password },
        { context: new HttpContext().set(SKIP_AUTH, true) }
      )
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
    this.http
      .post(`${this.apiBase}/auth/revoke`, {}, { withCredentials: true })
      .pipe(
        catchError((err) => {
          console.error('AuthService revoke error', err);
          return of(null);
        }),
        finalize(() => {
          this.clearSession();
          this.router.navigate(['/signin'], {
            queryParams: { returnUrl: this.router.url }
          });
        })
      )
      .subscribe();
  }

  isAuthenticated(): boolean {
    const token = this.accessToken;
    const accessValid = !!(token && this.tokenExpiryMs(token) > Date.now());

    if (!accessValid && this.profileSubject.value) {
      this.clearSession();
    }

    return accessValid;
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  refreshToken(): Observable<TokenResponse> {
    return this.http
      .post<TokenResponse>(
        `${this.apiBase}/auth/refresh`,
        {},
        { withCredentials: true, context: new HttpContext().set(SKIP_AUTH, true) }
      )
      .pipe(
        tap((res) => {
          if (res?.token) {
            this.applyTokens(res);
          }
        }),
        catchError((err) => this.handleError(err))
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
    // Keep in-memory access token for perf
    this.accessToken = res.token;

    // Decode and update profile (idempotent)
    const profile = this.decodeToken(res.token);
    this.updateProfile(profile);

    // Schedule auto refresh a little before expiry
    if (profile.exp) this.scheduleRefresh(profile.exp);
  }

  private restoreProfile(): void {
    // Keep any cached profile for UI continuity
    const cached = this.getStorage(this.PROFILE_KEY);
    if (cached) {
      try {
        this.profileSubject.next(JSON.parse(cached));
      } catch {
        this.storage.removeItem(this.PROFILE_KEY);
      }
    }

    // Attempt to refresh access token from refresh cookie
    this.beginRefresh();
    this.refreshToken()
      .pipe(take(1))
      .subscribe({
        next: (r) => this.endRefresh(r.token),
        error: (e) => {
          this.refreshFailed(e);
          this.logout();
        }
      });
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
          error: (e) => {
            this.refreshFailed(e);
            this.logout();
          }
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
      this.storage.setItem('deviceId', decoded.device_id);
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
    return this.storage.getItem(key);
  }

  private setStorage(key: string, value: string): void {
    this.storage.setItem(key, value);
  }

  private clearSession(): void {
    this.accessToken = null;
    this.profileSubject.next(null);

    // Remove cached profile only
    this.storage.removeItem(this.PROFILE_KEY);

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
      if (e.key === this.PROFILE_KEY) {
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
