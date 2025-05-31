import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable, of, tap } from 'rxjs';

import { environment } from '../environments/environment';
import { UserDiplomaStatus } from '../models/enums/user-diploma-status';
import { UserPaymentSchedule } from '../models/enums/user-payment-schedule';
import { User } from '../models/user';
import { UserRoleDetail } from '../models/user-role-detail';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private apiUrl = `${environment.apiUrl}/users`;
  private cachedUser: User | null = null;

  constructor(private http: HttpClient) { }

  getUser(): Observable<User> {
    if (this.cachedUser) {
      return of(this.cachedUser);
    }

    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      this.cachedUser = JSON.parse(storedUser);
      if (this.cachedUser) {
        return of(this.cachedUser);
      }
    }

    // Fetch from backend if not found locally
    return this.http.get<User>(`${this.apiUrl}/me`).pipe(
      tap(user => {
        this.cachedUser = user;
        localStorage.setItem('user', JSON.stringify(user));
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
  }
  refreshCachedUser(): void {
    this.getUser().subscribe();
  }  
  clearCachedUser(): void {
    this.cachedUser = null;
    localStorage.removeItem('user');
  }
  
  getUserByToken(recommendationToken: string): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/by-token/${recommendationToken}`);
  }

  getUserRoles(userId: string): Observable<UserRoleDetail[]> {
    return this.http.get<UserRoleDetail[]>(`${this.apiUrl}/${userId}/roles`);
  }

  getDiplomaStatus(): Observable<{ status: UserDiplomaStatus }> {
    return this.http.get<{ status: UserDiplomaStatus }>(`${this.apiUrl}/diploma-status`);
  }


  updateUser(user: Partial<User>, imageFile?: File): Observable<void> {
    // Prepare FormData
    const formData = new FormData();

    if (imageFile) {
      formData.append('profileImage', imageFile);
    }

    if (user.firstName) formData.append('firstName', user.firstName);
    if (user.lastName) formData.append('lastName', user.lastName);
    if (user.bio) formData.append('bio', user.bio);
    if (user.email) formData.append('email', user.email);
    if (user.dateOfBirth) formData.append('dateOfBirth', user.dateOfBirth);
    if (user.phoneNumber) formData.append('phoneNumber', user.phoneNumber);
    if (user.skypeId) formData.append('skypeId', user.skypeId);
    if (user.hangoutId) formData.append('hangoutId', user.hangoutId);
    if (user.timeZoneId) formData.append('timeZoneId', user.timeZoneId);

    if (user.address) {
      if (user.address.formattedAddress) formData.append('address.formattedAddress', user.address.formattedAddress);
      if (user.address.streetAddress) formData.append('address.streetAddress', user.address.streetAddress);
      if (user.address.city) formData.append('address.city', user.address.city);
      if (user.address.state) formData.append('address.state', user.address.state);
      if (user.address.country) formData.append('address.country', user.address.country);
      if (user.address.postalCode) formData.append('address.postalCode', user.address.postalCode);
      if (user.address.latitude) formData.append('address.latitude', user.address.latitude.toString());
      if (user.address.longitude) formData.append('address.longitude', user.address.longitude.toString());
    }

    if (user.profileVerified) {
      formData.append('profileVerified', user.profileVerified.join(','));
    }

    if (user.lessonsCompleted) {
      formData.append('lessonsCompleted', user.lessonsCompleted);
    }

    if (user.evaluations !== undefined) {
      formData.append('evaluations', String(user.evaluations));
    }

    if (user.recommendationToken) {
      formData.append('recommendationToken', user.recommendationToken);
    }

    if (user.isStripeConnected !== undefined) {
      formData.append('isStripeConnected', String(user.isStripeConnected));
    }

    return this.http.put<void>(`${this.apiUrl}/me`, formData);
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
    return this.http.put<void>(`${this.apiUrl}/change-password`, {
      oldPassword,
      newPassword,
      confirmNewPassword
    });
  }

  requestPasswordReset(email: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/request-reset-password`, { email: email });
  }

  resetPassword(data: { token: string; newPassword: string }): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/reset-password`, data);
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
