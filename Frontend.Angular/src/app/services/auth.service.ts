import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router, UrlTree } from '@angular/router';
import { defer, from, Observable, of, throwError } from 'rxjs';
import { catchError, finalize, shareReplay, switchMap } from 'rxjs/operators';
import { OAuthService } from 'angular-oauth2-oidc';

import { SessionService } from './session.service';

import { environment } from '../environments/environment';
import { INCLUDE_CREDENTIALS, SKIP_AUTH } from '../interceptors/auth.interceptor';
import { RegisterUserRequest } from '../models/register-user-request';
import { RegisterUserResponseDto } from '../models/register-user-response';
import { UserProfile } from '../models/UserProfile';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = environment.apiUrl;
  private refresh$?: Observable<string>;

  constructor(
    private readonly oauth: OAuthService,
    private readonly http: HttpClient,
    private readonly router: Router,
    private readonly sessionService: SessionService,
  ) {
    this.oauth.configure({
      issuer: environment.baseApiUrl,
      clientId: environment.clientId,
      responseType: 'code',
      scope: 'openid profile email offline_access',
      redirectUri: environment.redirectUri,
      postLogoutRedirectUri: environment.postLogoutRedirectUri
    });
  }

  /** Called once at app startup */
  init(): Promise<void> {
    return this.oauth.loadDiscoveryDocumentAndTryLogin({
      onTokenReceived: ctx => console.info('✅ Token received', ctx)
    }).then(() => {
      if (this.oauth.getRefreshToken()) {
        this.oauth.setupAutomaticSilentRefresh({ checkInterval: 60 });
      }
    });
  }

  isAuthenticated(): boolean {
    return this.oauth.hasValidAccessToken();
  }

  waitForRefresh(): Observable<unknown> {
    return this.refresh$ ? this.refresh$ : of(null);
  }

  getAccessToken(): string | null {
    return this.oauth.getAccessToken();
  }

  getValidAccessToken(): Observable<string> {
    if (this.oauth.hasValidAccessToken()) {
      return of(this.oauth.getAccessToken() as string);
    }

    if (!this.refresh$) {
      this.refresh$ = defer(() => from(this.oauth.refreshToken())).pipe(
        switchMap(() => {
          const token = this.oauth.getAccessToken();
          return token ? of(token) : throwError(() => new Error('No access token'));
        }),
        catchError(err => {
          this.logout();
          return throwError(() => err);
        }),
        finalize(() => {
          this.refresh$ = undefined;
        }),
        shareReplay({ bufferSize: 1, refCount: false })
      );
    }

    return this.refresh$;
  }

  startLogin(returnUrl = this.router.url): void {
    this.oauth.initCodeFlow(returnUrl);
  }

  logout(): void {
    const sessionId = this.decode()?.sessionId;
    const revoke$ = sessionId
      ? this.sessionService.revokeSession(sessionId)
      : of(void 0);

    revoke$
      .pipe(
        finalize(() => {
          this.oauth.logOut({
            logoutUrl: `${environment.baseApiUrl}connect/logout`,
            postLogoutRedirectUri: environment.postLogoutRedirectUri,
          });
        }),
      )
      .subscribe();
  }

  register(data: RegisterUserRequest): Observable<RegisterUserResponseDto> {
    return this.http.post<RegisterUserResponseDto>(
      `${this.api}/users/register`,
      data,
      {
        context: new HttpContext().set(SKIP_AUTH, true).set(INCLUDE_CREDENTIALS, true),
      },
    );
  }

  private normalizeClaim(value: unknown): string[] {
    if (Array.isArray(value)) {
      return value as string[];
    }
    if (typeof value === 'string') {
      return [value];
    }
    return [];
  }

  decode(): UserProfile | null {
    const claims = this.oauth.getIdentityClaims() as Record<string, unknown> | null;
    if (!claims || typeof claims !== 'object' || !('sub' in claims)) {
      return null;
    }

    try {
      const profile: UserProfile = {
        id: claims['sub'] as string,
        email: claims['email'] as string,
        firstName: claims['given_name'] as string,
        lastName: claims['family_name'] as string,
        fullName: claims['name'] as string,
        timeZoneId: claims['timezone'] as string,
        ipAddress: claims['ip_address'] as string,
        imageUrl: claims['image'] as string,
        deviceId: claims['device_id'] as string,
        sessionId: (claims['sid'] ?? claims['session_id']) as string,
        country: claims['country'] as string,
        city: claims['city'] as string,
        roles: this.normalizeClaim(claims['role']),
        permissions: this.normalizeClaim(claims['permission']),
        exp: claims['exp'] as number,
      };
      return profile;
    } catch {
      return null;
    }
  }

  redirectToSignIn(returnUrl: string = this.router.url): UrlTree {
    return this.router.createUrlTree(['/signin'], { queryParams: { returnUrl } });
  }
}
