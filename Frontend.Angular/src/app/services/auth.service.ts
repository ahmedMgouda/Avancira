import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router, UrlTree } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';
import { Observable, Subject, from, of, throwError } from 'rxjs';
import { finalize, map, catchError, tap, switchMap } from 'rxjs/operators';

import { environment } from '../environments/environment';
import { INCLUDE_CREDENTIALS, SKIP_AUTH } from '../interceptors/auth.interceptor';
import { RegisterUserRequest } from '../models/register-user-request';
import { RegisterUserResponseDto } from '../models/register-user-response';
import { TokenResponse } from '../models/token-response';
import { SocialProvider } from '../models/social-provider';
import { UserProfile } from '../models/UserProfile';
import { SessionService } from './session.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = environment.apiUrl;
  private refresh$?: Subject<unknown>;

  constructor(
    private readonly oauth: OAuthService,
    private readonly http: HttpClient,
    private readonly router: Router,
    private readonly sessionService: SessionService,
  ) {
    this.oauth.configure({
      issuer: environment.baseApiUrl,
      clientId: 'frontend',
      responseType: 'code',
      scope: 'api offline_access',
      redirectUri: `${environment.frontendUrl}/signin-callback`,
    });

    this.oauth.requester = (
      method: string,
      url: string,
      body: unknown,
      headers?: HttpHeaders | { [header: string]: string | string[] }
    ) =>
      this.http.request(method, url, {
        body,
        headers,
        withCredentials: true,
        context: new HttpContext().set(SKIP_AUTH, true).set(INCLUDE_CREDENTIALS, true),
      });
  }

  init(): Promise<void> {
    return this.oauth
      .loadDiscoveryDocumentAndTryLogin()
      .then(() => this.oauth.setupAutomaticSilentRefresh());
  }

  isAuthenticated(): boolean {
    return this.oauth.hasValidAccessToken();
  }

  waitForRefresh(): Observable<unknown> {
    return this.refresh$ ? this.refresh$.asObservable() : of(null);
  }

  getAccessToken(): string | null {
    return this.oauth.getAccessToken();
  }

  getValidAccessToken(): Observable<string> {
    if (this.oauth.hasValidAccessToken()) {
      return of(this.oauth.getAccessToken() as string);
    }

    if (!this.refresh$) {
      this.refresh$ = new Subject<unknown>();

      from(this.oauth.refreshToken())
        .pipe(
          catchError(err => {
            this.logout();
            this.refresh$?.error(err);
            this.refresh$ = undefined;
            return throwError(() => err);
          }),
        )
        .subscribe({
          next: () => {
            this.refresh$?.next(null);
            this.refresh$?.complete();
            this.refresh$ = undefined;
          },
          error: () => {
            /* no-op */
          },
        });
    }

    return this.waitForRefresh().pipe(map(() => this.oauth.getAccessToken() as string));
  }

  login(
    email: string,
    password: string,
    returnUrl = this.router.url
  ): Observable<TokenResponse> {
    return this.http
      .post<TokenResponse>(
        `${this.api}/auth/login`,
        { email, password },
        {
          context: new HttpContext()
            .set(SKIP_AUTH, true)
            .set(INCLUDE_CREDENTIALS, true),
        }
      )
      .pipe(
        switchMap(resp =>
          from(this.oauth.processIdToken(resp.token)).pipe(
            switchMap(() => from(this.oauth.tryLogin())),
            tap(() => this.router.navigateByUrl(returnUrl)),
            map(() => resp),
          ),
        ),
        catchError(err => throwError(() => err)),
      );
  }

  startLogin(returnUrl = this.router.url, provider?: SocialProvider): void {
    this.oauth.customQueryParams = provider ? { provider } : {};
    this.oauth.initCodeFlow(returnUrl);
  }

  logout(navigate = true): void {
    const sessionId = this.decode()?.sessionId;
    const revoke$ = sessionId
      ? this.sessionService.revokeSession(sessionId)
      : of(void 0);

    revoke$
      .pipe(
        finalize(() => {
          this.oauth.logOut();
          if (navigate) {
            this.router.navigateByUrl(this.redirectToSignIn());
          }
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

