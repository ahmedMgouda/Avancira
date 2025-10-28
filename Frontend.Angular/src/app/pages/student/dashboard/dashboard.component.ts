import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

// Services
// Models
import { LeaveReviewComponent } from '../../components/leave-review/leave-review.component';
import { ModalComponent } from '../../components/modal/modal.component';
import { ProfileImageComponent } from '../../components/profile-image/profile-image.component';

import { ChatService } from '../../services/chat.service';
import { EvaluationService } from '../../services/evaluation.service';
import { ListingService } from '../../services/listing.service';
import { PaymentService } from '../../services/payment.service';

// Components
// Pipes
import { TimeAgoPipe } from "../../pipes/time-ago.pipe";

import { Message } from '../../models/chat';
import { Listing } from '../../models/listing';
import { Review } from '../../models/review';
import { Transaction } from '../../models/transaction';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, FormsModule, ModalComponent, LeaveReviewComponent, ProfileImageComponent, TimeAgoPipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  messages: Message[] = [];
  reviewsPending: Review[] = [];
  transactions: Transaction[] = [];
  listings: Listing[] = [];
  selectedRevieweeId!: string;

  constructor(
    private chatService: ChatService,
    private paymentService: PaymentService,
    private evaluationService: EvaluationService,
    private listingService: ListingService
  ) { }

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.chatService.getChatsLastMessage().subscribe({
      next: (data) => (this.messages = data),
      error: (err) => console.error('Failed to load chat messages:', err),
    });
    this.evaluationService.getAllReviews().subscribe({
      next: (data) => (this.reviewsPending = data.pendingReviews),
      error: (err) => console.error('Failed to load reviews:', err),
    });
    this.paymentService.getPaymentHistory().subscribe({
      next: (response) => (this.transactions = response.transactions),
      error: (err) => console.error('Failed to load payment history:', err),
    });
    this.listingService.getListings().subscribe({
      next: (data) => (this.listings = data.results),
      error: (err) => console.error('Failed to fetch listings:', err),
    });
  }


  isModalOpen = false;

  openModal(revieweeId: string): void {
    this.selectedRevieweeId = revieweeId;
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }
}
