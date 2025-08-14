import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, of, Subscription, throwError, timer } from 'rxjs';
import { catchError, map, switchMap, tap, filter, take } from 'rxjs/operators';
import { jwtDecode } from 'jwt-decode';

import { NotificationService } from './notification.service';

import { environment } from '../environments/environment';
import { UserProfile } from '../models/UserProfile';

export interface TokenResponse {
    token: string;
    refreshToken?: string;
    refreshTokenExpiryTime?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService implements OnDestroy {
    private readonly PROFILE_KEY = 'user_profile';
    private readonly ACCESS_TOKEN_KEY = 'access_token'; // ✅ new
    private readonly REFRESH_TOKEN_KEY = 'refresh_token';
    private readonly REFRESH_EXPIRY_KEY = 'refresh_token_expiry';
    private readonly apiBase = environment.apiUrl;

    private accessToken: string | null = null;
    private refreshTimerSub: Subscription | null = null;
    private profileSubject = new BehaviorSubject<UserProfile | null>(null);
    profile$ = this.profileSubject.asObservable();

    private isRefreshing = false;
    private refreshTokenSubject = new BehaviorSubject<string | null>(null);

    constructor(
        private http: HttpClient,
        private router: Router,
        private notificationService: NotificationService
    ) {
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
        // Notify all waiters that refresh has failed to avoid hanging requests
        this.refreshTokenSubject.error(error ?? new Error('Token refresh failed'));
        // Reset subject so future refresh attempts can be awaited again
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

    /** ----------- PUBLIC API ----------- **/

    login(email: string, password: string): Observable<UserProfile> {
        return this.http.post<TokenResponse>(`${this.apiBase}/auth/token`, { email, password }).pipe(
            switchMap(res => {
                if (!res?.token) return throwError(() => new Error('No token returned from server'));

                this.applyTokens(res);
                const profile = this.decodeToken(res.token);

                return this.loadPermissions().pipe(
                    map(perms => {
                        this.updateProfile({ ...profile, permissions: perms });
                        return this.profileSubject.value!;
                    })
                );
            }),
            catchError(err => this.handleError(err))
        );
    }

    logout(): void {
        if (!this.accessToken && !this.profileSubject.value) return;
        this.clearSession();
        this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
    }

    isAuthenticated(): boolean {
        return !!(
            (this.accessToken && this.tokenExpiryMs(this.accessToken) > Date.now()) ||
            (
                this.getStorage(this.REFRESH_TOKEN_KEY) &&
                this.getStorage(this.REFRESH_EXPIRY_KEY) &&
                Date.parse(this.getStorage(this.REFRESH_EXPIRY_KEY)!) > Date.now()
            )
        );
    }

    getAccessToken(): string | null {
        return this.accessToken || localStorage.getItem('access_token');
    }

    refreshToken(): Observable<TokenResponse> {
        const token = this.accessToken || this.getStorage(this.ACCESS_TOKEN_KEY); // ✅ ensure token present
        const refreshToken = this.getStorage(this.REFRESH_TOKEN_KEY);

        if (!token || !refreshToken) {
            this.logout();
            return throwError(() => new Error('Missing token or refresh token'));
        }

        return this.http.post<TokenResponse>(`${this.apiBase}/auth/refresh`, {
            Token: token,
            RefreshToken: refreshToken
        }).pipe(
            tap(res => {
                if (res?.token) {
                    this.applyTokens(res, false);
                    const { exp } = this.decodeToken(res.token);
                    if (exp) this.scheduleRefresh(exp);
                }
            }),
            catchError(err => {
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

    /** ----------- PRIVATE HELPERS ----------- **/

    private applyTokens(res: TokenResponse, scheduleRefresh = true): void {
        this.accessToken = res.token;
        this.storeTokens(res);

        const profile = this.decodeToken(res.token);
        this.updateProfile(profile);

        if (scheduleRefresh && profile.exp) {
            this.scheduleRefresh(profile.exp);
        }
    }

    private storeTokens(res: TokenResponse) {
        if (res.token) this.setStorage(this.ACCESS_TOKEN_KEY, res.token); // ✅ persist access token
        if (res.refreshToken) this.setStorage(this.REFRESH_TOKEN_KEY, res.refreshToken);
        if (res.refreshTokenExpiryTime) this.setStorage(this.REFRESH_EXPIRY_KEY, res.refreshTokenExpiryTime);
    }

    private restoreProfile(): void {
        const storedProfile = this.getStorage(this.PROFILE_KEY);
        const storedToken = this.getStorage(this.ACCESS_TOKEN_KEY); // ✅ restore token
        if (storedToken) {
            this.accessToken = storedToken;
        }

        if (storedProfile) {
            try {
                const profile: UserProfile = JSON.parse(storedProfile);
                this.profileSubject.next(profile);
                if (profile.exp) this.scheduleRefresh(profile.exp);
            } catch {
                localStorage.removeItem(this.PROFILE_KEY);
            }
        }
    }

    private updateProfile(profile: Partial<UserProfile>): void {
        const updated = { ...this.profileSubject.value, ...profile } as UserProfile;
        this.profileSubject.next(updated);
        this.setStorage(this.PROFILE_KEY, JSON.stringify(updated));
    }

    private scheduleRefresh(exp: number) {
        this.cancelRefresh();
        const refreshInMs = Math.max(0, exp * 1000 - Date.now() - 2 * 60_000);
        if (refreshInMs > 0) {
            this.refreshTimerSub = timer(refreshInMs).pipe(switchMap(() => this.refreshToken())).subscribe();
        }
    }

    private cancelRefresh() {
        this.refreshTimerSub?.unsubscribe();
        this.refreshTimerSub = null;
    }

    private decodeToken(token: string): UserProfile {
        const decoded: any = jwtDecode(token);
        return {
            id: decoded.sub,
            email: decoded.email,
            firstName: decoded.given_name || '',
            lastName: decoded.family_name || '',
            fullName: decoded.fullName || `${decoded.given_name || ''} ${decoded.family_name || ''}`.trim(),
            timeZoneId: decoded.timeZoneId,
            ipAddress: decoded.ipAddress,
            imageUrl: decoded.image_url,
            roles: this.toArray(decoded.role),
            permissions: this.toArray(decoded.permissions),
            exp: decoded.exp
        };
    }

    private tokenExpiryMs(token: string): number {
        try {
            return (jwtDecode<any>(token).exp || 0) * 1000;
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
        return !!list?.some(v => items.includes(v));
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

    private clearSession() {
        this.accessToken = null;
        this.profileSubject.next(null);
        [
            this.PROFILE_KEY,
            this.REFRESH_TOKEN_KEY,
            this.REFRESH_EXPIRY_KEY,
            this.ACCESS_TOKEN_KEY // ✅ clear persisted token
        ].forEach(k => localStorage.removeItem(k));
        this.cancelRefresh();
        this.notificationService.stopConnection();
    }

    private handleError(err: HttpErrorResponse | any) {
        console.error('AuthService error', err);
        return throwError(() => new Error(err?.error?.message || err?.message || 'Authentication error'));
    }

    ngOnDestroy(): void {
        this.cancelRefresh();
        this.notificationService.stopConnection();
    }
}
