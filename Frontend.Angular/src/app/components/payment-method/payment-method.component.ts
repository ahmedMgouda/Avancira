import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ManageCardsComponent } from '../manage-cards/manage-cards.component';

import { PaymentService } from '../../services/payment.service';

import { Card } from '../../models/card';
import { UserCardType } from '../../models/enums/user-card-type';

@Component({
  selector: 'app-payment-method',
  imports: [CommonModule, FormsModule, ManageCardsComponent],
  templateUrl: './payment-method.component.html',
  styleUrl: './payment-method.component.scss'
})
export class PaymentMethodComponent {
  @Input() totalPrice: number = 0;
  @Input() listingId: number | null = null;
  @Input() returnUrl: string = '/payment-result';
  @Input() onApproval!: (data: any) => void;
  @Output() paymentConfirmed = new EventEmitter<void>();
  CardType: UserCardType = UserCardType.Paying;
  @Input() selectedCard: Card | null = null;
  @Output() selectedCardChange = new EventEmitter<Card | null>();
  selectedPaymentMethod: string = 'card';

  constructor(
    private paymentService: PaymentService
  ) { }

  onCardSelected(card: Card | null): void {
    this.selectedCard = card;
    this.selectedCardChange.emit(this.selectedCard);
  }

  onPaymentMethodChange(method: string) {
    this.selectedPaymentMethod = method;
    if (method === 'paypal') {
      this.paymentService.loadPayPalScript().then(() => {
        return this.paymentService.renderPayPalButton(
          '#paypal-button-container',
          'PayPal',
          this.listingId ?? 0,
          this.totalPrice,
          '/messages',
          this.onApproval
        );
      });  
    }
  }
  
  confirmAndPay() {
    if (this.selectedPaymentMethod === 'card') {
      console.log('Processing card payment...');
      this.paymentConfirmed.emit();
    } else {
      alert('Please complete the payment via PayPal.');
    }
  }
}
