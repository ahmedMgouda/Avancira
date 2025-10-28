import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LandingService {
  private readonly apiUrl = `${environment.bffBaseUrl}/api/landing`;

  constructor(private http: HttpClient) {}

  getCourseStats(): Observable<any> {
    return this.http.get(`${this.apiUrl}/stats`);
  }

  getCategories(): Observable<any> {
    return this.http.get(`${this.apiUrl}/categories`);
  }

  getListings(): Observable<any> {
    return this.http.get(`${this.apiUrl}/courses`);
  }

  getTrendingListings(): Observable<any> {
    return this.http.get(`${this.apiUrl}/trending-courses`);
  }

  getInstructors(): Observable<any> {
    return this.http.get(`${this.apiUrl}/instructors`);
  }

  getJobLocations(): Observable<any> {
    return this.http.get(`${this.apiUrl}/job-locations`);
  }

  getStudentReviews(): Observable<any> {
    return this.http.get(`${this.apiUrl}/student-reviews`);
  }
}
