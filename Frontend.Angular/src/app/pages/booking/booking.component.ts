import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { PaymentMethodComponent } from '../../components/payment-method/payment-method.component';

import { AlertService } from '../../services/alert.service';
import { LessonService } from '../../services/lesson.service';
import { ListingService } from '../../services/listing.service';
import { PaymentService } from '../../services/payment.service';

import { Card } from '../../models/card';
import { TransactionPaymentMethod } from '../../models/enums/transaction-payment-method';
import { UserCardType } from '../../models/enums/user-card-type';
import { Listing } from '../../models/listing';
import { Proposition } from '../../models/proposition';

@Component({
  selector: 'app-booking',
  imports: [CommonModule, FormsModule, PaymentMethodComponent],
  templateUrl: './booking.component.html',
  styleUrl: './booking.component.scss'
})
export class BookingComponent implements OnInit {
  CardType: UserCardType = UserCardType.Paying;
  selectedCard: Card | null = null;
  listing!: Listing;
  loading: boolean = true;
  selectedDate: Date = new Date(); // Selected lesson date
  selectedTime: string = ''; // Selected lesson time
  lessonDuration: number = 1; // Default 1 hour
  totalPrice: number = 0; // Total price calculated dynamically
  minDate: Date = new Date(); // Minimum selectable date

  constructor(
    private paymentService: PaymentService,
    private alertService: AlertService,
    private lessonService: LessonService,
    private route: ActivatedRoute,
    private listingService: ListingService,
    private router: Router
  ) {
    this.handlePayment = this.handlePayment.bind(this);
  }

  ngOnInit(): void {
    // Fetch the listing ID from route parameters
    this.route.paramMap.subscribe((params) => {
      const listingId = Number(params.get('id'));
      if (!isNaN(listingId)) {
        this.loadListing(listingId);
      } else {
        console.error('Listing ID not found');
        this.loading = false;
      }
    });
  }
  
  loadListing(listingId: number): void {
    this.listingService.getListing(listingId).subscribe({
      next: (listing) => {
        this.listing = listing;
        this.totalPrice = listing.rates.hourly; // Default price for 1 hour
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to fetch listing:', err);
      }
    });
  }

  updateTotalPrice(): void {
    if (this.listing) {
      this.totalPrice = this.lessonDuration * this.listing.rates.hourly;
    }
  }

  payWithSelectedCard(): void {
    if (!this.selectedDate || !this.selectedTime) {
      this.alertService.warningAlert('Please select a date and time for the lesson.');
      return;
    }
    if (!this.selectedCard) {
      this.alertService.warningAlert('Please select a payment card.');
      return;
    }

    this.handlePayment(null);
  }

  handlePayment(data: any) {
    console.log('PayPal Payment Approved!', data);

    // Navigate to payment page with listing ID
    const proposition: Proposition = {
      paymentMethod: data ? TransactionPaymentMethod.PayPal : TransactionPaymentMethod.Stripe,
      payPalPaymentId: data ? data.paymentID : null,
      date: new Date(`${this.selectedDate}T${this.selectedTime}:00`),
      duration: this.lessonDuration,
      price: this.totalPrice,
      listingId: this.listing.id,
      studentId: null,
    };

    this.lessonService.proposeLesson(proposition).subscribe({
      next: () => {
        this.alertService.successAlert('Lesson proposed successfully!', 'Success');
        this.router.navigate(['/messages']);
      },
      error: (err) => {
        console.error('Failed to propose lesson:', err);
        this.alertService.errorAlert('Failed to propose lesson. Please try again.', 'Error');
      },
    });
  }
}
