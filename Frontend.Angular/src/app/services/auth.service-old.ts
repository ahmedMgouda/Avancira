import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, map, Observable, tap, throwError } from 'rxjs';

import { NotificationService } from './notification.service';
import { UserService } from './user.service';

import { environment } from '../environments/environment';
import { ApiResponse } from '../models/api-response';


@Injectable({
  providedIn: 'root',
})
export class AuthServiceold {
  private apiUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient, private userService: UserService, private notificationService: NotificationService) { }

  register(
    email: string,
    password: string,
    confirmPassword: string,
    referralToken: string | null
  ): Observable<string | null> {
    const userName = email;
    const timeZoneId = Intl.DateTimeFormat().resolvedOptions().timeZone;
    
    return this.http.post<{ errors?: string[] }>(`${this.apiUrl}/register`, {
      firstName: "User", // Default value since form doesn't collect this
      lastName: "Account", // Default value since form doesn't collect this
      email,
      userName,
      password,
      confirmPassword,
      referralToken,
      timeZoneId
    })
      .pipe(
        map(() => null), // Registration successful
        catchError((error: HttpErrorResponse) => {
          if (error.status === 400 && error.error?.errors?.length) {
            return throwError(() => new Error(error.error.errors[0])); // Return first validation error
          }
          return throwError(() => new Error('An unexpected error occurred. Please try again.'));
        })
      );
  }

  confirmEmail(userId: string, token: string): Observable<ApiResponse<string>> {
    return this.http.get<ApiResponse<string>>(`${this.apiUrl}/ConfirmEmail`, {
      params: { userId, token },
      headers: { 'Content-Type': 'application/json' },
    })
      .pipe(
        catchError((error: HttpErrorResponse) => {
          const errorMessage = error.error?.message || error.error?.details || `HTTP error ${error.status}`;
          console.error('Email confirmation error:', errorMessage);
          return throwError(() => new Error(errorMessage || "An error occurred during email confirmation."));
        })
      );
  }

  login(email: string, password: string): Observable<{ token: string; roles: string[] } | null> {
    return this.http
      .post<{ token: string; roles: string[] }>(`${this.apiUrl}/login`, { email, password })
      .pipe(
        tap(response => {
          if (response) {
            this.saveToken(response.token);
            this.saveEmail(email);
            this.saveRoles(response.roles);
            this.userService.refreshCachedUser();
          }
        }),
        map(response => response ?? null),
        catchError((error: HttpErrorResponse) => {
          console.error('Login error:', error.message);
          return throwError(() => new Error('Login failed. Please try again.'));
        })
      );
  }


  

  socialLogin(provider: string, token: string): Observable<{ token: string; roles: string[]; isRegistered: boolean }> {
    return this.http
      .post<{ token: string; roles: string[]; isRegistered: boolean }>(`${this.apiUrl}/social-login`, { provider, token })
      .pipe(
        catchError((error: HttpErrorResponse) => {
          console.error('Social login error:', error.message);
          return throwError(() => new Error('Social login failed. Please try again.'));
        })
      );
  }

  isLoggedIn() {
    return !!localStorage.getItem('token');
  }

  saveToken(token: string): void {
    localStorage.setItem('token', token);
  }

  saveEmail(email: string): void {
    localStorage.setItem('email', email);
  }

  saveRoles(roles: string[]): void {
    localStorage.setItem('roles', JSON.stringify(roles));
  }

  saveCurrentRole(role: string): void {
    localStorage.setItem('currentRole', role);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  getRoles(): string[] {
    const roles = localStorage.getItem('roles');
    return roles ? JSON.parse(roles) : [];
  }

  getCurrentRole(): string | null {
    return localStorage.getItem('currentRole');
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('email');
    localStorage.removeItem('roles');
    localStorage.removeItem('currentRole');
    this.userService.clearCachedUser();
    this.notificationService.stopConnection();
  }
}
