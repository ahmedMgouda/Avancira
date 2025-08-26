import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router, UrlTree } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';
import { Observable, from, of } from 'rxjs';
import { map } from 'rxjs/operators';

import { environment } from '../environments/environment';
import { INCLUDE_CREDENTIALS, SKIP_AUTH } from '../interceptors/auth.interceptor';
import { RegisterUserRequest } from '../models/register-user-request';
import { RegisterUserResponseDto } from '../models/register-user-response';
import { SocialProvider } from '../models/social-provider';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = environment.apiUrl;

  constructor(
    private readonly oauth: OAuthService,
    private readonly http: HttpClient,
    private readonly router: Router,
  ) {
    this.oauth.configure({
      issuer: environment.baseApiUrl,
      clientId: 'frontend',
      responseType: 'code',
      scope: 'api offline_access',
      redirectUri: `${environment.frontendUrl}/signin-callback`,
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
    return of(null);
  }

  getAccessToken(): string | null {
    return this.oauth.getAccessToken();
  }

  getValidAccessToken(): Observable<string> {
    if (this.oauth.hasValidAccessToken()) {
      return of(this.oauth.getAccessToken() as string);
    }
    return from(this.oauth.refreshToken()).pipe(
      map(() => this.oauth.getAccessToken() as string)
    );
  }

  startLogin(returnUrl = this.router.url, provider?: SocialProvider): Promise<void> {
    this.oauth.customQueryParams = provider ? { provider } : {};
    return Promise.resolve(this.oauth.initCodeFlow(returnUrl));
  }

  logout(navigate = true): void {
    this.oauth.logOut();
    if (navigate) {
      this.router.navigateByUrl(this.redirectToSignIn());
    }
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

  redirectToSignIn(returnUrl: string = this.router.url): UrlTree {
    return this.router.createUrlTree(['/signin'], { queryParams: { returnUrl } });
  }
}

