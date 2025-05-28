import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../environments/environment';
import { Review } from '../models/review';

@Injectable({
  providedIn: 'root'
})
export class EvaluationService {
  private apiUrl = `${environment.apiUrl}/evaluations`;

  constructor(private http: HttpClient) { }

  submitReview(review: Review): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/review`, review);
  }

  submitRecommendation(recommendation: Review): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/recommendation`, recommendation);
  }
  getAllReviews(): Observable<{ pendingReviews: Review[]; receivedReviews: Review[]; sentReviews: Review[], recommendations: Review[] }> {
    return this.http.get<{ pendingReviews: Review[]; receivedReviews: Review[]; sentReviews: Review[], recommendations: Review[] }>(this.apiUrl);
  }
}
