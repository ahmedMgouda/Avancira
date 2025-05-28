import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

import { PaymentService } from '../../services/payment.service';

import { PaymentHistory } from '../../models/payment-history';
import { Transaction } from '../../models/transaction';

@Component({
  selector: 'app-invoices',
  imports: [CommonModule],
  templateUrl: './invoices.component.html',
  styleUrl: './invoices.component.scss'
})
export class InvoicesComponent {
  paymentHistory: PaymentHistory | null = null;
  invoices: Transaction[] = [];

  constructor(
    private paymentService: PaymentService,
  ) {
  }

  // 1. Lifecycle Hooks
  ngOnInit(): void {
    this.loadPaymentHistory();
  }

  // 3. Payment Management
  loadPaymentHistory(): void {
    this.paymentService.getPaymentHistory().subscribe({
      next: (data) => 
        {
          this.paymentHistory = data,
          this.invoices = this.paymentHistory.invoices;
        },
      error: (err) => console.error('Failed to fetch payment history', err)
    });
  }

  printInvoice(): void {
    window.print();
  }  
}
