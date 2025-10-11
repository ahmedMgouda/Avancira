import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, map, Observable, of, switchMap, tap, throwError } from 'rxjs';

import { environment } from '../environments/environment';
import { UserDiplomaStatus } from '../models/enums/user-diploma-status';
import { UserPaymentSchedule } from '../models/enums/user-payment-schedule';
import { ResetPasswordRequest } from '../models/reset-password-request';
import { User } from '../models/user';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private apiUrl = `${environment.apiUrl}/users`;
  private cachedUser: User | null = null;
  private userSubject = new BehaviorSubject<User | null>(null);
  user$ = this.userSubject.asObservable();

  private cooldownMs = 30000;
  private lastRequestPasswordReset = 0;
  private lastResetPassword = 0;

  private checkCooldown(lastTime: number): number {
    return this.cooldownMs - (Date.now() - lastTime);
  }

  private buildCooldownError(remaining: number, action: string): Error {
    const seconds = Math.ceil(remaining / 1000);
    return new Error(`Please wait ${seconds}s before ${action}.`);
  }

  getRequestPasswordResetCooldown(): number {
    const remaining = this.checkCooldown(this.lastRequestPasswordReset);
    return remaining > 0 ? Math.ceil(remaining / 1000) : 0;
  }

  getResetPasswordCooldown(): number {
    const remaining = this.checkCooldown(this.lastResetPassword);
    return remaining > 0 ? Math.ceil(remaining / 1000) : 0;
  }

  constructor(private http: HttpClient) { }

  getUser(): Observable<User> {
    if (this.cachedUser) {
      this.userSubject.next(this.cachedUser);
      return of(this.cachedUser);
    }

    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      this.cachedUser = JSON.parse(storedUser);
      if (this.cachedUser) {
        if ((this.cachedUser as any).profileImagePath && !(this.cachedUser as any).imageUrl) {
          (this.cachedUser as any).imageUrl = (this.cachedUser as any).profileImagePath;
        }
        this.userSubject.next(this.cachedUser);
        return of(this.cachedUser);
      }
    }

    // Fetch from backend if not found locally
    return this.http.get<User>(`${this.apiUrl}/me`).pipe(
      tap(user => {
        this.cachedUser = user;
        localStorage.setItem('user', JSON.stringify(user));
        this.userSubject.next(user);
      })
    );
  }

  getTimeZone(): Observable<string> {
    if (this.cachedUser?.timeZoneId) {
      return of(this.cachedUser.timeZoneId);
    }

    return this.getUser().pipe(
      map(user => user.timeZoneId || Intl.DateTimeFormat().resolvedOptions().timeZone)
    );
  }

  setUser(user: User): void {
    this.cachedUser = user;
    localStorage.setItem('user', JSON.stringify(user));
    this.userSubject.next(user);
  }
  refreshCachedUser(): void {
    this.clearCachedUser();
    this.getUser().subscribe();
  }

  forceRefreshUser(): Observable<User> {
    this.clearCachedUser();
    return this.http.get<User>(`${this.apiUrl}/me`).pipe(
      tap(user => {
        this.cachedUser = user;
        localStorage.setItem('user', JSON.stringify(user));
        this.userSubject.next(user);
      })
    );
  }
  clearCachedUser(): void {
    this.cachedUser = null;
    localStorage.removeItem('user');
    this.userSubject.next(null);
  }
  
  getUserByToken(recommendationToken: string): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/by-token/${recommendationToken}`);
  }

  getDiplomaStatus(): Observable<{ status: UserDiplomaStatus }> {
    return this.http.get<{ status: UserDiplomaStatus }>(`${this.apiUrl}/diploma-status`);
  }

  updateUser(user: Partial<User>, imageFile?: File): Observable<void> {
    return this.getUser().pipe(
      switchMap(currentUser => {
        // Prepare FormData to match UpdateUserDto
        const formData = new FormData();

        // Add the user ID - this is required by UpdateUserDto
        // Use the provided user.id or fall back to the current user's id
        const userId = user.id || currentUser.id;
        if (userId) {
          formData.append('Id', userId);
        }

        // Map frontend fields to backend DTO fields
        if (imageFile) {
          formData.append('Image', imageFile);  // Backend expects 'Image', not 'profileImage'
        }

        if (user.firstName) formData.append('FirstName', user.firstName);
        if (user.lastName) formData.append('LastName', user.lastName);
        if (user.email) formData.append('Email', user.email);
        if (user.phoneNumber) formData.append('PhoneNumber', user.phoneNumber);
        if (user.skypeId) formData.append('SkypeId', user.skypeId);
        if (user.hangoutId) formData.append('HangoutId', user.hangoutId);
        if (user.bio) formData.append('Bio', user.bio);
        if (user.timeZoneId) formData.append('TimeZoneId', user.timeZoneId);
        
        if (user.dateOfBirth) {
          // Since dateOfBirth is now a string, just pass it directly
          formData.append('DateOfBirth', user.dateOfBirth);
        }

        // Address fields
        if (user.address) {
          if (user.address.formattedAddress) formData.append('AddressFormattedAddress', user.address.formattedAddress);
          if (user.address.streetAddress) formData.append('AddressStreetAddress', user.address.streetAddress);
          if (user.address.city) formData.append('AddressCity', user.address.city);
          if (user.address.state) formData.append('AddressState', user.address.state);
          if (user.address.country) formData.append('AddressCountry', user.address.country);
          if (user.address.postalCode) formData.append('AddressPostalCode', user.address.postalCode);
          if (user.address.latitude) formData.append('AddressLatitude', user.address.latitude.toString());
          if (user.address.longitude) formData.append('AddressLongitude', user.address.longitude.toString());
        }

        // Profile verification and stats
        if (user.profileVerified) {
          formData.append('ProfileVerified', user.profileVerified.join(','));
        }

        if (user.lessonsCompleted !== null && user.lessonsCompleted !== undefined) {
          formData.append('LessonsCompleted', user.lessonsCompleted.toString());
        }

        if (user.evaluations !== undefined) {
          formData.append('Evaluations', String(user.evaluations));
        }

        if (user.recommendationToken) {
          formData.append('RecommendationToken', user.recommendationToken);
        }

        if (user.isStripeConnected !== undefined) {
          formData.append('IsStripeConnected', String(user.isStripeConnected));
        }

        // Add DeleteCurrentImage flag if needed
        formData.append('DeleteCurrentImage', 'false');

        return this.http.put<void>(`${this.apiUrl}/profile`, formData).pipe(
          tap(() => {
            // Clear cached user data after successful update
            this.clearCachedUser();
          }),
          switchMap(() => {
            // Fetch fresh user data from the server
          return this.http.get<User>(`${this.apiUrl}/me`).pipe(
            tap(updatedUser => {
              // Update the cache with fresh data
              this.cachedUser = updatedUser;
              localStorage.setItem('user', JSON.stringify(this.cachedUser));
              this.userSubject.next(updatedUser);
            }),
              map(() => void 0) // Convert to void to match return type
            );
          })
        );
      })
    );
  }

  getCompensationPercentage(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/compensation-percentage`);
  }

  getPaymentPreference(): Observable<UserPaymentSchedule> {
    return this.http.get<UserPaymentSchedule>(`${this.apiUrl}/payment-schedule`);
  }

  updateCompensationPercentage(newPercentage: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/compensation-percentage`, { percentage: newPercentage });
  }

  updatePaymentPreference(paymentPreference: UserPaymentSchedule): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/payment-schedule`,
      { paymentSchedule: paymentPreference }
    );
  }

  changePassword(oldPassword: string, newPassword: string, confirmNewPassword: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/change-password`, {
      Password: oldPassword,
      NewPassword: newPassword,
      ConfirmNewPassword: confirmNewPassword
    });
  }

  requestPasswordReset(email: string): Observable<void> {
    const remaining = this.checkCooldown(this.lastRequestPasswordReset);
    if (remaining > 0) {
      return throwError(() => this.buildCooldownError(remaining, 'requesting again'));
    }
    this.lastRequestPasswordReset = Date.now();
    return this.http.post<void>(
      `${this.apiUrl}/forgot-password`,
      { email },
      { context: new HttpContext() }
    );
  }

  resetPassword(data: ResetPasswordRequest): Observable<void> {
    const remaining = this.checkCooldown(this.lastResetPassword);
    if (remaining > 0) {
      return throwError(() => this.buildCooldownError(remaining, 'trying again'));
    }
    this.lastResetPassword = Date.now();
    return this.http.post<void>(
      `${this.apiUrl}/reset-password`,
      {
        userId: data.userId,
        password: data.password,
        confirmPassword: data.confirmPassword,
        token: data.token,
      },
      { context: new HttpContext()}
    );
  }

  submitDiploma(diplomaFile: File): Observable<void> {
    const formData = new FormData();
    formData.append('diplomaFile', diplomaFile);

    return this.http.put<void>(`${this.apiUrl}/submit-diploma`, formData);
  }

  deleteAccount(): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/me`);
  }

}
