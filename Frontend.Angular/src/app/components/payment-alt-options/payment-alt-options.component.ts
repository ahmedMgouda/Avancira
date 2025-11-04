import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { loadStripe } from '@stripe/stripe-js';

import { AuthService } from '../../core/services/auth.service';
import { ConfigService } from '../../services/config.service';
import { PaymentService } from '../../services/payment.service';

import { ConfigKey } from '../../models/config-key';



@Component({
  selector: 'app-payment-alt-options',
  imports: [CommonModule],
  templateUrl: './payment-alt-options.component.html',
  styleUrl: './payment-alt-options.component.scss'
})
export class PaymentAltOptionsComponent implements OnInit {
  listingId!: string;
  paymentMethod: 'card' | 'paypal' = 'paypal';
  subscription = {
    title: 'Student Pass',
    subtitle: 'Non-Binding Monthly Subscription',
    price: 69,
    benefits: [
      'Your card is debited only if the tutor accepts your request.',
      'Contact unlimited tutors in all subjects with this pass.'
    ]
  };
  stripePromise: Promise<any> | null = null; 

  constructor(
    private router: Router,
    private paymentService: PaymentService,
    private configService: ConfigService,
    private authService: AuthService,
  ) { }


  ngOnInit(): void {
    this.authService.isAuthenticated();
    this.stripePromise = loadStripe(this.configService.get(ConfigKey.StripePublishableKey));

    this.paymentService.loadPayPalScript().then(() => {
      this.renderPayPalButton();
    });
  }

  async onPaymentMethodChange(method: 'card' | 'paypal'): Promise<void> {
    this.paymentMethod = method;
    if (method === 'card') {
      await this.handleStripePayment();
    }
  }

  async handleStripePayment(): Promise<void> {
    const stripe = await this.stripePromise;
    if (!stripe) {
      console.error('Stripe could not be initialized');
      return;
    }

    try {
      // Call backend to create a Stripe Checkout Session
      this.paymentService.createPayment("Stripe", this.listingId, this.subscription.price).subscribe({
        next: (session: { paymentId: string, approvalUrl: string }) => {
          if (session.approvalUrl) {
            // Redirect to the Stripe Checkout Session URL
            window.location.href = session.approvalUrl;
          } else {
            console.error('Failed to retrieve Stripe session URL');
          }
        },
        error: (error) => {
          console.error('Error creating Stripe session:', error);
        },
      });
    } catch (error) {
      console.error('Error initiating Stripe payment:', error);
    }
  }

  renderPayPalButton(): void {
    const paypal = (window as any).paypal;

    if (paypal && paypal.Buttons) {
      paypal.Buttons({
        createOrder: () => {
          return new Promise<string>((resolve, reject) => {
            this.paymentService.createPayment("PayPal", this.listingId, 69.00).subscribe({
              next: (order) => {
                if (!order || !order.paymentId) {
                  console.error('Payment Id not returned from the server.');
                  reject('Payment Id not returned from the server.');
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
          this.router.navigate(['/payment-result'], {
            queryParams: {
              success: true,
              listingId: this.listingId,
              gateway: 'PayPal',
              paymentId: data.orderID,
            },
          });
        },
        onError: (err: any) => {
          console.error('PayPal Button Error:', err);
        },
      }).render('#paypal-button-container');
    } else {
      console.error('PayPal SDK not loaded.');
    }
  }
}
