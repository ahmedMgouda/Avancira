import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { ManageCardsComponent } from '../../components/manage-cards/manage-cards.component';

import { AlertService } from '../../services/alert.service';
import { PaymentService } from '../../services/payment.service';
import { SubscriptionService } from '../../services/subscription.service';
import { UserService } from '../../services/user.service';

import { UserCardType } from '../../models/enums/user-card-type';
import { UserPaymentSchedule } from '../../models/enums/user-payment-schedule';
import { PaymentHistory } from '../../models/payment-history';
import { User } from '../../models/user';

@Component({
  selector: 'app-payments',
  imports: [CommonModule, FormsModule, ManageCardsComponent],
  templateUrl: './payments.component.html',
  styleUrl: './payments.component.scss'
})
export class PaymentsComponent {
  // Enums and Data Models
  CardType = UserCardType;
  PaymentSchedule = UserPaymentSchedule;

  // States
  activeTab: string = 'profile';
  activeSubTab: string = 'history';
  profile: User | null = null;
  payment: PaymentHistory | null = null;


  // Payment
  isPayPalConnected: boolean = false;
  paymentPreference: UserPaymentSchedule = UserPaymentSchedule.PerLesson;
  compensationPercentage: number = 50;

  // Subscription Management
  subscriptionDetails: any = null;

  constructor(
    private alertService: AlertService,
    private userService: UserService,
    private paymentService: PaymentService,
    private subscriptionService: SubscriptionService,
    private route: ActivatedRoute,
    private router: Router
  ) {

  }
  // 1. Lifecycle Hooks
  ngOnInit(): void {
    this.route.queryParams.subscribe((params) => {
      this.activeTab = params['section'] || 'profile';
      this.activeSubTab = params['detail'] || 'history';

      const paypalCode = params['code'];
      if (paypalCode) {
        this.handlePayPalAuth(paypalCode);
      }  
    });

    this.fetchCompensationPercentage();
    this.loadPaymentPreference();
    this.loadUserProfile();
    this.loadPaymentHistory();
    this.loadSubscriptionDetails();

    this.loadPayPalScript().then(paypal => {
      paypal.use(['login'], function (login: any) {
        login.render({
          "appid": "AeGRDU26BUwiEgw_MZr4GV3FR8Ge5-0MVM8XOEcTMUNo-ZbhsN7jTQk0W68_Ts-fSIxDDBYyhSrhCu54",
          "authend": "sandbox",
          "scopes": "openid email https://uri.paypal.com/services/paypalattributes",
          "containerid": "paypal-button-container",
          "responseType": "code",
          "locale": "en-us",
          "buttonType": "CWP",
          "buttonShape": "rectangle",
          "buttonSize": "md",
          "fullPage": "true",
          "returnurl": "https://www.avancira.com/dashboard/payments?section=payments&detail=receiving"
        });
      });
    });

  }

  loadPayPalScript(): Promise<any> {
    return new Promise((resolve, reject) => {
      if ((window as any).paypal) {
        resolve((window as any).paypal);
        return;
      }

      const script = document.createElement('script');
      script.src = 'https://www.paypalobjects.com/js/external/api.js';
      script.async = true;
      script.onload = () => {
        const paypal = (window as any).paypal;
        if (paypal) {
          resolve(paypal);
        } else {
          reject('PayPal OAuth SDK not available after loading.');
        }
      };
      script.onerror = () => reject('PayPal OAuth SDK failed to load.');
      document.body.appendChild(script);
    });
  }
  // Function to send the code to backend
  handlePayPalAuth(authCode: string): void {
    this.paymentService.connectPayPalAccount(authCode).subscribe({
      next: (res) => {
        console.log('PayPal account linked successfully:', res);
        this.isPayPalConnected = true;
        this.alertService.successAlert('Your PayPal account is linked!', 'Success');
      },
      error: (err) => {
        console.error('Failed to exchange PayPal code:', err);
        this.alertService.errorAlert('Failed to link your PayPal account.', 'Error');
      }
    });
  }

  // 2. User Profile Management
  loadUserProfile(): void {
    this.userService.getUser().subscribe({
      next: (user) => {
        this.profile = user;
        this.isStripeConnected = user.isStripeConnected;
        this.isPayPalConnected = user.isPayPalConnected;
      },
      error: (err) => console.error('Failed to fetch user profile', err)
    });
  }

  // 3. Payment Management
  loadPaymentHistory(): void {
    this.paymentService.getPaymentHistory().subscribe({
      next: (data) => this.payment = data,
      error: (err) => console.error('Failed to fetch payment history', err)
    });
  }

  initializePayPalButton(): void {
    this.paymentService.loadPayPalScript().then(() => {
      (window as any).paypal
        .Buttons({
          style: { layout: 'vertical', label: 'paypal' },
          onApprove: () => {
            this.paymentService.addPayPalAccount('example@paypal.com').subscribe(() => {
              this.alertService.successAlert('Your PayPal account has been linked successfully!', 'Success');
              this.isPayPalConnected = true;
            });
          },
          onError: (err: any) => {
            console.error('Error linking PayPal account:', err);
            this.alertService.errorAlert('Failed to link your PayPal account. Please try again.', 'Error');
          },
        })
        .render('#paypal-button-container');
    });
  }

  loadPaymentPreference(): void {
    this.userService.getPaymentPreference().subscribe({
      next: (preference) => { this.paymentPreference = preference },
      error: (err) => console.error('Failed to load payment preference', err),
    });
  }

  // Subscription Management
  loadSubscriptionDetails(): void {
    this.subscriptionService.getSubscriptionDetails().subscribe({
      next: (details) => {
        this.subscriptionDetails = details;
      },
      error: (err) => console.error('Failed to load subscription details', err)
    });
  }

  subscribeNow() {
    this.router.navigate(['/payment'], {
      queryParams: { referrer: this.router.url }
    });
  }

  updateSubscription(): void {
    this.subscriptionService.updateSubscription().subscribe({
      next: () => {
        this.alertService.successAlert('Subscription updated successfully!', 'Success');
      },
      error: (err) => {
        console.error('Failed to update subscription', err);
        this.alertService.errorAlert('Failed to update subscription. Please try again.', 'Error');
      },
    });
  }

  cancelSubscription(): void {
    this.subscriptionService.cancelSubscription().subscribe({
      next: () => {
        this.loadSubscriptionDetails();
        this.alertService.successAlert('Subscription cancelled successfully!', 'Success');
      },
      error: (err) => {
        console.error('Failed to cancel subscription', err);
        this.alertService.errorAlert('Failed to cancel subscription. Please try again.', 'Error');
      },
    });
  }

  editBillingFrequency() {
    throw new Error('Method not implemented.');
  }
  cancelPlan(): void {
    this.subscriptionService.cancelSubscription().subscribe({
      next: () => {
        this.alertService.successAlert('Subscription cancelled successfully!', 'Success');
      },
      error: (err) => {
        console.error('Failed to cancel subscription', err);
        this.alertService.errorAlert('Failed to cancel subscription. Please try again.', 'Error');
      },
    });
  }
  switchPlans() {
    throw new Error('Method not implemented.');
  }
  changePaymentMethod() {
    throw new Error('Method not implemented.');
  }


  savePaymentPreference(): void {
    this.userService.updatePaymentPreference(this.paymentPreference).subscribe({
      next: () => { },
      error: (err) => console.error('Failed to update payment preference', err),
    });
  }

  // 5. Compensation Management
  fetchCompensationPercentage(): void {
    this.userService.getCompensationPercentage().subscribe({
      next: (percentage) => this.compensationPercentage = percentage,
      error: (err) => console.error('Error fetching compensation percentage', err)
    });
  }

  adjustCompensation(value: number): void {
    const newPercentage = this.compensationPercentage + value;
    if (newPercentage >= 0 && newPercentage <= 100) {
      this.userService.updateCompensationPercentage(newPercentage).subscribe({
        next: () => this.compensationPercentage = newPercentage,
        error: (err) => console.error('Error updating compensation percentage', err)
      });
    }
  }

  // 7. Helpers & Event Handlers
  onSubTabChange(subTab: string): void {
    this.activeSubTab = subTab;
    if (this.activeSubTab === 'receiving') {
      setTimeout(() => this.initializePayPalButton(), 1000);
    }
  }


  isStripeConnected = false;
  amount: number = 0;
  currency: string = 'aud';

  connectStripe(): void {
    this.paymentService.connectStripeAccount().subscribe((data) => {
      window.location.href = data.url;
    });
  }

  createPayout(): void {
    if (this.amount > 0) {
      this.paymentService.createPayout(this.amount, this.currency).subscribe(() => {

      });
    }
  }
}
