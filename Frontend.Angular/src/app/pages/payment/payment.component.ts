import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { PaymentMethodComponent } from "../../components/payment-method/payment-method.component";

import { AlertService } from '../../services/alert.service';
import { SubscriptionService } from '../../services/subscription.service';
import { AuthService } from '../../services/auth.service';

import { Card } from '../../models/card';
import { TransactionPaymentMethod } from '../../models/enums/transaction-payment-method';
import { TransactionPaymentType } from '../../models/enums/transaction-payment-type';
import { UserCardType } from '../../models/enums/user-card-type';

@Component({
  selector: 'app-payment',
  imports: [CommonModule, FormsModule, PaymentMethodComponent],
  templateUrl: './payment.component.html',
  styleUrls: ['./payment.component.scss']
})
export class PaymentComponent implements OnInit {
  CardType: UserCardType = UserCardType.Paying;
  referrer: string | null = null;
  loading = true;
  isLoggedIn = false;
  selectedCard: Card | null = null;

  constructor(
    private alertService: AlertService,
    private route: ActivatedRoute,
    private router: Router,
    private subscriptionService: SubscriptionService,
    private authService: AuthService
  ) {
    this.handlePayment = this.handlePayment.bind(this);
  }

  ngOnInit(): void {
    this.isLoggedIn = this.authService.isLoggedIn();

    this.route.queryParams.subscribe((params) => {
      this.referrer = params['referrer'] || '/';
    });

    this.route.paramMap.subscribe(() => {
      this.checkSubscriptionStatus();
    });
  }


  onCardSelected(card: Card | null): void {
    this.selectedCard = card;
  }

  checkSubscriptionStatus(): void {
    this.subscriptionService.checkActiveSubscription().subscribe({
      next: (response: { isActive: boolean }) => {
        if (response.isActive) {
          const referrerUrl = new URL(this.referrer || '/', window.location.origin);
          const queryParams = Object.fromEntries(referrerUrl.searchParams.entries());
          delete queryParams['referrer'];
          queryParams['success'] = 'true';
          this.router.navigate([referrerUrl.pathname], {
            queryParams
          });
        }
      },
      error: (err) => {
        console.error('Error checking subscription status:', err);
      },
    });
  }

  payWithSelectedCard(): void {
    if (!this.selectedCard) {
      this.alertService.warningAlert('Please select a card to proceed.');
      return;
    }

    this.handlePayment(null);
  }

  handlePayment(data: any) {
    console.log('PayPal Payment Approved!', data);

    const subscriptionRequest = {
      payPalPaymentId: data ? data.paymentID : null,
      promoCode: this.promoCode,
      amount: this.totalPrice,
      paymentMethod: this.selectedCard
      ? TransactionPaymentMethod.Stripe
      : TransactionPaymentMethod.PayPal,
      description: this.selectedCard
        ? `Card ending in ${this.selectedCard.last4}`
        : `PayPal - Transaction ID: ${data.orderID}`,
      paymentType: TransactionPaymentType.StudentMembership,
      billingFrequency: this.selectedPlan
    };

    // Capture the payment and then create the subscription
    this.subscriptionService.createSubscription(subscriptionRequest).subscribe({
      next: () => {
        this.alertService.successAlert('Subscription created successfully!', 'Success');

        const referrerUrl = new URL(this.referrer || '/', window.location.origin);
        const queryParams = Object.fromEntries(referrerUrl.searchParams.entries());
        delete queryParams['referrer'];
        queryParams['success'] = 'true';

        this.router.navigate([referrerUrl.pathname], { queryParams });
      },
      error: (err) => {
        console.error('Error creating subscription:', err);
        this.alertService.errorAlert('Failed to process subscription. Please try again.', 'Subscription Failed');

        const referrerUrl = new URL(this.referrer || '/', window.location.origin);
        const queryParams = Object.fromEntries(referrerUrl.searchParams.entries());
        delete queryParams['referrer'];
        queryParams['success'] = 'false';

        this.router.navigate([referrerUrl.pathname], { queryParams });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/']);
  }

  goToLogin(): void {
    const currentUrl = this.router.url;
    this.router.navigate(['/signin'], {
      queryParams: { returnUrl: currentUrl },
    });
  }


  selectedPlan: 'Monthly' | 'Yearly' = 'Monthly';

  planOptions = [
    { key: 'Monthly', label: 'Monthly Plan', price: 69, period: 'month', description: 'Enjoy full access to our services with a monthly commitment.' },
    { key: 'Yearly', label: '12-Month Plan', price: 69 * 12, period: 'year', description: 'Includes 3 extra months free! Pay for 12 months and get 15 months of access.' }
  ];

  promoCode: string = '';
  discountAmount: number = 0;
  totalPrice: number = this.getSelectedPlanPrice();
  promoMessage: string = '';

  getSelectedPlan(): any {
    return this.planOptions.find(plan => plan.key === this.selectedPlan) || this.planOptions[0];
  }

  getSelectedPlanPrice(): number {
    return this.getSelectedPlan().price;
  }

  updatePlan(): void {
    this.totalPrice = this.getSelectedPlanPrice();
    this.applyPromoCode();
  }

  applyPromoCode(): void {
    if (!this.promoCode.trim()) {
      this.promoMessage = "Please enter a promo code.";
      this.discountAmount = 0;
      this.totalPrice = this.getSelectedPlanPrice();
      return;
    }

    this.subscriptionService.validatePromoCode(this.promoCode).subscribe({
      next: (response) => {
        const planPrice = this.getSelectedPlanPrice();
        this.discountAmount = response.discountAmount || (planPrice * response.discountPercentage) / 100;
        this.totalPrice = Math.max(0, planPrice - this.discountAmount);
        this.promoMessage = `Promo applied! You saved $${this.discountAmount.toFixed(2)}.`;
      },
      error: () => {
        this.promoMessage = "Invalid or expired promo code.";
        this.discountAmount = 0;
        this.totalPrice = this.getSelectedPlanPrice();
      },
    });
  }
}
