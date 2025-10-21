import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../environments/environment';
import { PromoCodeValidation } from '../models/promo-code-validation';
import { SubscriptionDetails } from '../models/subscription-details';
import { SubscriptionRequest } from '../models/subscription-request';
import { SubscriptionStatusCheck } from '../models/subscription-status-check';

@Injectable({
  providedIn: 'root'
})
export class SubscriptionService {
  private readonly apiUrl = `${environment.bffBaseUrl}/api/subscriptions`;

  constructor(private http: HttpClient) { }
  
  createSubscription(request: SubscriptionRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/create`, request);
  }

  checkActiveSubscription(): Observable<SubscriptionStatusCheck> {
    return this.http.get<SubscriptionStatusCheck>(`${this.apiUrl}/check-active`);
  }

  getSubscriptionDetails(): Observable<SubscriptionDetails> {
    return this.http.get<SubscriptionDetails>(`${this.apiUrl}/details`);
  }
  
  validatePromoCode(promoCode: string): Observable<PromoCodeValidation> {
    return this.http.get<PromoCodeValidation>(`${this.apiUrl}/validate-promo`, {
      params: { promoCode }
    });
  }

  updateSubscription(): Observable<void> {
    throw new Error('Method not implemented.');
  }

  cancelSubscription(): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/cancel`);
  }
}
