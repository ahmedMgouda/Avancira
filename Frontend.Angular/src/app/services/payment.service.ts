import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';

import { ConfigService } from './config.service';
import { ConfigKey } from '../models/config-key';

import { environment } from '../environments/environment';
import { Card } from '../models/card';
import { UserCardType } from '../models/enums/user-card-type';
import { PaymentHistory } from '../models/payment-history';
import { PaymentResult } from '../models/payment-result';
import { PayPalConnectionResult } from '../models/paypal-connection-result';
import { StripeConnectionResult } from '../models/stripe-connection-result';

@Injectable({
  providedIn: 'root',
})
export class PaymentService {
  private apiUrl = `${environment.apiUrl}/payments`;

  constructor(
    private router: Router,
    private http: HttpClient,
    private configService: ConfigService
  ) { }

  getPaymentHistory(): Observable<PaymentHistory> {
    return this.http.get<PaymentHistory>(`${this.apiUrl}/history`);
  }

  createPayment(gateway: string, listingId: string | null, amount: number): Observable<PaymentResult> {
    const returnUrl = `${environment.frontendUrl}/payment-result?success=true&listingId=${listingId}&gateway=${gateway}`;
    const cancelUrl = `${environment.frontendUrl}/payment-result?success=false&listingId=${listingId}&gateway=${gateway}`;
    const body = { gateway, amount, returnUrl, cancelUrl, listingId };

    return this.http.post<PaymentResult>(`${this.apiUrl}/create-payment`, body);
  }

  capturePayment(gateway: string, paymentId: string): Observable<void> {
    const body = { gateway, paymentId };

    return this.http.post<void>(`${this.apiUrl}/capture-payment`, body);
  }

  addPayPalAccount(payPalEmail: string): Observable<void> {
    const body = { payPalEmail };

    return this.http.post<void>(`${this.apiUrl}/add-paypal-account`, body);
  }

  loadPayPalScript(): Promise<void> {
    return new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = `https://www.paypal.com/sdk/js?client-id=${this.configService.get(ConfigKey.PayPalClientId)}&currency=AUD`;
      script.async = true;
      script.onload = () => resolve();
      script.onerror = () => reject('PayPal SDK could not be loaded.');
      document.body.appendChild(script);
    });
  }

  /**
 * Renders the PayPal button
 * @param containerId - ID of the HTML container where PayPal button should be rendered
 * @param paymentMethod - The payment method (e.g., "PayPal")
 * @param listingId - ID of the listing/item being purchased
 * @param amount - Total payment amount
 * @param returnUrl - The URL to redirect to after payment, with optional query params
 */
  renderPayPalButton(
    containerId: string,
    paymentMethod: string,
    listingId: string,
    amount: number,
    returnUrl: string,
    onApprove: (data: any) => void | null
  ): void {
    const paypal = (window as any).paypal;
    if (!paypal || !paypal.Buttons) {
      console.error('PayPal SDK not loaded.');
      return;
    }

    paypal.Buttons({
      createOrder: () => {
        return new Promise<string>((resolve, reject) => {
          this.createPayment(paymentMethod, listingId, amount).subscribe({
            next: (order) => {
              if (!order || !order.paymentId) {
                console.error('Payment ID not returned from the server.');
                reject('Payment ID not returned from the server.');
                return;
              }
              resolve(order.paymentId);
            },
            error: (error) => {
              console.error('Error creating order:', error);
              reject(error);
            },
          });
        });
      },
      onApprove: (data: any) => {
        // Use the passed onApprove callback if provided, otherwise use default logic
        if (onApprove) {
          onApprove(data);
        } else {
          const urlWithParams = this.buildReturnUrl(returnUrl, {
            success: true,
            listingId: listingId,
            gateway: paymentMethod,
            paymentId: data.orderID
          });
          this.router.navigateByUrl(urlWithParams);
        }
      },
      onError: (err: any) => {
        console.error('PayPal Button Error:', err);
      },
    }).render(containerId);
  }

  connectPayPalAccount(authCode: string): Observable<PayPalConnectionResult> {
    const body = { authCode };
    return this.http.post<PayPalConnectionResult>(`${this.apiUrl}/connect-paypal-account`, body);
  }
      
  /**
   * Helper function to construct a return URL with query parameters.
   * @param baseUrl - Base return URL (e.g., "/payment-result")
   * @param params - Query parameters to append
   * @returns - Fully constructed URL with query params
   */
  private buildReturnUrl(baseUrl: string, params: Record<string, any>): string {
    const queryString = new URLSearchParams(params).toString();
    return `${baseUrl}?${queryString}`;
  }

  getSavedCards(): Observable<Card[]> {
    return this.http.get<Card[]>(`${this.apiUrl}/saved-cards`);
  }

  saveCard(stripeToken: string, purpose: UserCardType): Observable<void> {
    const body = { stripeToken, purpose };

    return this.http.post<void>(`${this.apiUrl}/save-card`, body);
  }

  removeCard(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/remove-card/${id}`);
  }

  connectStripeAccount(): Observable<StripeConnectionResult> {
    return this.http.get<StripeConnectionResult>(`${this.apiUrl}/connect-stripe-account`);
  }

  createPayout(amount: number, currency: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/create-payout`, { amount, currency });
  }
}
