import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { ChatService } from '../../../services/chat.service';
import { LessonService } from '../../../services/lesson.service';
import { ListingService } from '../../../services/listing.service';
import { SubscriptionService } from '../../../services/subscription.service';

import { Listing } from '../../../models/listing';
import { SendMessage } from '../../../models/send-message';

@Component({
  selector: 'app-listing',
  imports: [CommonModule, FormsModule],
  templateUrl: './listing.component.html',
  styleUrls: ['./listing.component.scss'],
})
export class ListingComponent implements OnInit {
  listing!: Listing;
  newMessage: string = '';
  lessonDate: Date = new Date(); // ISO date string
  lessonDuration: number = 1; // Duration in hours
  lessonPrice: number = 0; // Price in dollars
  loading: boolean = true;
  messageSuccess: boolean = false; // Indicates whether the message was sent successfully
  showMessageSection: boolean = false; // Toggle between message and propose lesson sections

  constructor(
    private route: ActivatedRoute,
    private listingService: ListingService,
    private chatService: ChatService,
    private lessonService: LessonService,
    private subscriptionService: SubscriptionService,
    private router: Router
  ) { }

  ngOnInit(): void {
    // Fetch the tutor ID from route params
    this.route.paramMap.subscribe((params) => {
      const listingId = params.get('id');
      if (listingId) {
        this.loadListing(listingId);
      } else {
        console.error('Tutor ID not found');
        this.loading = false;
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/']);
  }

  loadListing(listingId: string): void {
    this.listingService.getListing(listingId).subscribe({
      next: (listing: Listing) => {
        this.listing = listing;
        this.loading = false;
      },
      error: (err: unknown) => {
        console.error('Failed to fetch listing:', err);
      },
    });
  }

  sendMessage(): void {
    if (this.newMessage.trim()) {
      const payload: SendMessage = {
        listingId: this.listing?.id!,
        recipientId: "",
        content: this.newMessage,
      };

      this.chatService.sendMessage(payload).subscribe({
        next: () => {
          this.messageSuccess = true;
          this.showMessageSection = false; // Hide the message section
          this.newMessage = '';
          setTimeout(() => {
            this.messageSuccess = false;
          }, 3000); // Keep the message success indicator for 3 seconds
        },
        error: (err: unknown) => {
          console.error('Failed to send message:', err);
        },
      });
    }
  }

  navigateToPayment(action: string): void {
    if (this.listing && this.listing.id) {
      this.subscriptionService.checkActiveSubscription().subscribe({
        next: (response: { isActive: boolean }) => {
          if (response.isActive) {
            if (action == 'booking')
            {
              this.router.navigate(['/booking', this.listing.id]);
            }
            else if (action == 'message') {
              this.showMessageSection = true;
            }
          }
          else {
            this.router.navigate(['/payment'], {
              queryParams: { referrer: this.router.url }
            });
          }
        },
        error: (err: unknown) => {
          console.error('Error checking subscription status:', err);
        },
      });
    }
  }
}
