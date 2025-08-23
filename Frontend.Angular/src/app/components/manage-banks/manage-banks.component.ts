import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { loadStripe, Stripe, StripeIbanElement } from '@stripe/stripe-js';

import { ConfigService } from '../../services/config.service';
import { PaymentService } from '../../services/payment.service';
import { ConfigKey } from '../../models/config-key';

@Component({
  selector: 'app-manage-banks',
  imports: [CommonModule, FormsModule],
  templateUrl: './manage-banks.component.html',
  styleUrl: './manage-banks.component.scss'
})
export class ManageBanksComponent implements OnInit {
  stripe: Stripe | null = null;
  ibanElement: StripeIbanElement | null = null;
  accountHolderName: string = '';
  savedBanks: any[] = [];

  constructor(
    private paymentService: PaymentService,
    private configService: ConfigService
  ) { }

  ngOnInit() {
    this.initializeStripeIban();
    this.loadSavedBanks();
  }

  async initializeStripeIban(): Promise<void> {
    this.stripe = await loadStripe(this.configService.get(ConfigKey.StripePublishableKey));

    if (!this.stripe) {
      console.error('Stripe is not initialized.');
      return;
    }

    const elements = this.stripe.elements();
    this.ibanElement = elements.create('iban', {
      supportedCountries: ['SEPA'],
      placeholderCountry: 'US',
      style: {
        base: {
          fontSize: '16px',
          color: '#32325d',
        },
      },
    });
    this.ibanElement.mount('#iban-element');
  }

  loadSavedBanks(): void {
    // this.paymentService.getSavedBanks().subscribe({
    //   next: (banks) => (this.savedBanks = banks),
    //   error: (err) => console.error('Failed to load saved banks', err),
    // });
  }

  async saveBankAccount(event: Event): Promise<void> {
    event.preventDefault();

    // if (!this.stripe || !this.ibanElement) {
    //   alert('Stripe is not initialized.');
    //   return;
    // }
    
    // const { token, error } = await this.stripe.createToken(this.ibanElement!, new  {
    //   account_holder_name: this.accountHolderName,
    //   account_holder_type: 'individual', // Or 'company'
    // });

    // if (error) {
    //   console.error('Error creating token:', error);
    //   alert('Failed to tokenize bank account.');
    //   return;
    // }

    // this.paymentService.saveBankToken(token!.id).subscribe({
    //   next: () => {
    //     alert('Bank account saved successfully.');
    //     this.loadSavedBanks();
    //     this.accountHolderName = '';
    //   },
    //   error: (err) => {
    //     console.error('Error saving bank account:', err);
    //     alert('Failed to save bank account.');
    //   },
    // });
  }

  removeBank(bankId: number): void {
    console.log(bankId);
    // this.paymentService.removeBank(bankId).subscribe({
    //   next: () => this.loadSavedBanks(),
    //   error: (err) => console.error('Failed to remove bank account', err),
    // });
  }
}
