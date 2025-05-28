import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { LeaveReviewComponent } from '../../components/leave-review/leave-review.component';
import { ModalComponent } from '../../components/modal/modal.component';

import { AlertService } from '../../services/alert.service';
import { AuthService } from '../../services/auth.service';
import { EvaluationService } from '../../services/evaluation.service';
import { UserService } from '../../services/user.service';

import { TimeAgoPipe } from '../../pipes/time-ago.pipe';

import { environment } from '../../environments/environment';
import { Review } from '../../models/review';
import { User } from '../../models/user';

@Component({
  selector: 'app-evaluations',
  imports: [CommonModule, FormsModule, ModalComponent, LeaveReviewComponent, TimeAgoPipe],
  templateUrl: './evaluations.component.html',
  styleUrl: './evaluations.component.scss'
})
export class EvaluationsComponent {
  pendingReviews: Review[] = [];
  receivedReviews: Review[] = [];
  sentReviews: Review[] = [];
  recommendations: Review[] = [];
  remainingReviews = 0;
  activeTab = 'reviews';
  activeSubTab = 'received';
  selectedRevieweeId!: string;
  recommendationLink: string = '';
  sponsorLink: string = '';

  constructor(
    private alertService: AlertService,
    private userService: UserService,
    private authService: AuthService,
    private evaluationService: EvaluationService
  ) { }

  ngOnInit(): void {
    this.loadAllReviews();
    // Fetch the logged-in user's ID or username
    this.userService.getUser().subscribe({
      next: (user: User) => {
        if (user.recommendationToken) {
          this.recommendationLink = `${environment.frontendUrl}/recommendation/${user.recommendationToken}`;
          this.sponsorLink = `${environment.frontendUrl}/signup?referral=${user.recommendationToken}`;
        }
      },
      error: (err) => {
        console.error('Failed to fetch user data:', err);
      }
    });
  }

  copyLink(): void {
    navigator.clipboard.writeText(this.recommendationLink).then(() => {
      this.alertService.successAlert('Link copied to clipboard!', 'Success');
    }).catch(err => {
      console.error('Could not copy link: ', err);
      this.alertService.errorAlert('Failed to copy link. Please try again.', 'Error');
    });
  }
  
  loadAllReviews(): void {
    this.evaluationService.getAllReviews().subscribe((data) => {
      this.pendingReviews = data.pendingReviews;
      this.receivedReviews = data.receivedReviews;
      this.sentReviews = data.sentReviews;
      this.recommendations = data.recommendations;
      // this.remainingReviews = data.pendingReviews.length;
    });
  }

  setActiveTab(tab: string): void {
    this.activeSubTab = tab;
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
