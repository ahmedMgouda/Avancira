import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { loadStripe } from '@stripe/stripe-js';

import { ManageCardsComponent } from '../../components/manage-cards/manage-cards.component';

import { AlertService } from '../../services/alert.service';
import { ConfigService } from '../../services/config.service';
import { PaymentService } from '../../services/payment.service';
import { SubscriptionService } from '../../services/subscription.service';

import { Card } from '../../models/card';
import { TransactionPaymentType } from '../../models/enums/transaction-payment-type';
import { UserCardType } from '../../models/enums/user-card-type';

@Component({
  selector: 'app-premium-subscription',
  imports: [CommonModule, FormsModule, ManageCardsComponent],
  templateUrl: './premium-subscription.component.html',
  styleUrl: './premium-subscription.component.scss'
})
export class PremiumSubscriptionComponent implements OnInit {
  subscription = {
    title: 'Premium Subscription',
    subtitle: 'Unlock premium features and boost your performance!',
    price: 99,
    benefits: [
      'Get higher visibility in search results.',
      'No commission on your earnings.',
      'Access priority customer support.',
      'Detailed listing performance statistics.',
    ],
  };

  CardType: UserCardType = UserCardType.Paying;
  isLoggedIn = false;
  paymentMethod: 'card' | 'paypal' = 'paypal';
  stripePromise: Promise<any> | null = null; 
  savedCards: Card[] = []; // Add saved cards property
  savedCardsAvailable = false; // Check if the user has saved cards
  selectedCard: Card | null = null; // Track the selected card

  constructor(
    private alertService: AlertService,
    private router: Router,
    private paymentService: PaymentService,
    private subscriptionService: SubscriptionService,
    private configService: ConfigService
  ) {}

  ngOnInit(): void {
    this.isLoggedIn = !!localStorage.getItem('token');
    this.stripePromise = loadStripe(this.configService.get('stripePublishableKey'));
  }

  goToLogin(): void {
    const currentUrl = this.router.url;
    this.router.navigate(['/signin'], { queryParams: { returnUrl: currentUrl } });
  }

  async handlePayment(): Promise<void> {
    if (this.paymentMethod === 'card') {
      await this.handleStripePayment();
    } else if (this.paymentMethod === 'paypal') {
      this.handlePayPalPayment();
    }
  }

  async handleStripePayment(): Promise<void> {
    const stripe = await this.stripePromise;
    if (!stripe) {
      console.error('Stripe could not be initialized');
      return;
    }

    try {
      this.paymentService.createPayment('Stripe', 0, this.subscription.price).subscribe({
        next: (session: { paymentId: string; approvalUrl: string }) => {
          if (session.approvalUrl) {
            window.location.href = session.approvalUrl; // Redirect to Stripe
          }
        },
        error: (err) => console.error('Error creating Stripe session:', err),
      });
    } catch (err) {
      console.error('Error initiating Stripe payment:', err);
    }
  }

  handlePayPalPayment(): void {
    this.paymentService.createPayment('PayPal', 0, this.subscription.price).subscribe({
      next: (order) => {
        if (order && order.approvalUrl) {
          window.location.href = order.approvalUrl; // Redirect to PayPal
        }
      },
      error: (err) => console.error('Error creating PayPal order:', err),
    });
  }

  
  loadSavedCards(): void {
    this.paymentService.getSavedCards().subscribe({
      next: (cards) => {
        this.savedCards = cards; // Populate savedCards
        this.savedCardsAvailable = cards.length > 0; // Update availability flag
      },
      error: (err) => {
        console.error('Error fetching saved cards:', err);
        this.savedCardsAvailable = false;
      },
    });
  }

  selectCard(card: Card): void {
    this.selectedCard = card;
  }

  onCardSelected(card: Card | null): void {
    this.selectedCard = card; // Update the selected card
  }

  payWithSelectedCard(): void {
    if (!this.selectedCard) {
      this.alertService.warningAlert('Please select a card to proceed.');
      return;
    }

    const subscriptionRequest = {
      amount: this.subscription.price, // Use the subscription price dynamically
      paymentMethod: `Card ending in ${this.selectedCard.last4}`, // Describe the payment method,
      paymentType: TransactionPaymentType.TutorMembership
    };

    // Call the subscription service to create a subscription
    this.subscriptionService.createSubscription(subscriptionRequest).subscribe({
      next: (response) => {
        console.log('Subscription created successfully:', response);
        this.alertService.successAlert('Subscription created successfully!', 'Success');
        this.router.navigate(['/payment-result'], {
          queryParams: {
            success: true,
            listingId: 0,
            gateway: 'Stripe',
            paymentType: TransactionPaymentType.TutorMembership
          }
        });
      },
      error: (err) => {
        console.error('Error creating subscription:', err);
        this.alertService.errorAlert('Failed to process payment. Please try again.', 'Payment Failed');
        this.router.navigate(['/payment-result'], {
          queryParams: {
            success: false,
            listingId: 0,
            gateway: 'Stripe',
            paymentType: TransactionPaymentType.TutorMembership
          }
        });
      },
    });
  }
}
