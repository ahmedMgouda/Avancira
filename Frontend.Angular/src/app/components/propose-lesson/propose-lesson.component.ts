import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { LessonService } from '../../services/lesson.service';
import { UserService } from '../../services/user.service';

import { TransactionPaymentMethod } from '../../models/enums/transaction-payment-method';
import { Listing } from '../../models/listing';
import { Proposition } from '../../models/proposition';


@Component({
  selector: 'app-propose-lesson',
  imports: [FormsModule, CommonModule],
  templateUrl: './propose-lesson.component.html',
  styleUrl: './propose-lesson.component.scss'
})
export class ProposeLessonComponent {
  @Input() listing!: Listing;
  @Input() studentId: string | null = null;
  @Output() onPropose = new EventEmitter<{ date: Date; duration: number; price: number }>();
  @Output() onClose = new EventEmitter<void>();
  minDateTime: Date = new Date()
  lessonDateTime: Date = new Date()
  lessonDuration: number = 1;
  lessonPrice: number = 0;
  proposeSuccess = false;
  paymentDetailsAvailable = false;
  isButtonDisabled = false;

  constructor(
    private lessonService: LessonService,
    private userService: UserService,
    private router: Router
  ) {}

  ngOnInit(): void {
    if (this.listing) {
      this.updateLessonPrice();
    } else {
      throw new Error('Listing input is required for ProposeLessonComponent.');
    }

    this.checkPaymentDetails();
  }

  /**
   * Checks if the user has added payment details.
   */
  checkPaymentDetails(): void {
    this.userService.getUser().subscribe({
      next: (user) => {
        this.paymentDetailsAvailable = user.isStripeConnected;
      },
      error: (err) => {
        console.error('Failed to check payment details:', err);
        this.paymentDetailsAvailable = false;
      },
    });
  }

  /**
   * Updates the lesson price based on the lesson duration and hourly rate.
   */
  updateLessonPrice(): void {
    if (this.listing && this.listing.rates.hourly) {
      this.lessonPrice = this.lessonDuration * this.listing.rates.hourly;
    }
  }
  
  proposeLesson(): void {
    if (!this.listing) {
      console.error('Tutor details not available');
      return;
    }

    if (this.lessonDateTime && this.lessonDuration && this.lessonPrice !== null) {
      const proposition: Proposition = {
        paymentMethod: TransactionPaymentMethod.Card,
        payPalPaymentId: null,
        date: this.lessonDateTime,
        duration: this.lessonDuration,
        price: this.lessonPrice,
        listingId: this.listing.id,
        studentId: this.studentId,
      };

      this.isButtonDisabled = true;

      this.lessonService.proposeLesson(proposition).subscribe({
        next: () => {
          this.onPropose.emit({
            date: this.lessonDateTime,
            duration: this.lessonDuration,
            price: this.lessonPrice,
          });
          this.proposeSuccess = true;
          setTimeout(() => {
            this.proposeSuccess = false;
            this.closeModal();
            this.router.navigate(['/messages']);
          }, 3000);
        },
        error: (err) => {
          console.error('Failed to propose lesson:', err);
          this.isButtonDisabled = false;
        },
      });
    }
  }

  /**
   * Navigate to the profile page to add payment details.
   */
  navigateToProfile(): void {
    this.router.navigate(['/profile'], { queryParams: { section: 'payments', detail: 'method' } });
  }

  
  closeModal(): void {
    this.onClose.emit();
  }
}
