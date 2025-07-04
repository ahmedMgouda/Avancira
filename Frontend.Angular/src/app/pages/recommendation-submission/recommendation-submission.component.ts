import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { AlertService } from '../../services/alert.service';
import { EvaluationService } from '../../services/evaluation.service';
import { UserService } from '../../services/user.service';

import { Review } from '../../models/review';

@Component({
  selector: 'app-recommendation-submission',
  imports: [CommonModule, FormsModule],
  templateUrl: './recommendation-submission.component.html',
  styleUrl: './recommendation-submission.component.scss'
})
export class RecommendationSubmissionComponent implements OnInit {
  recommendation: Review = {
    revieweeId: '',
    date: new Date(),
    name: '',
    subject: '',
    feedback: '',
    avatar: null,
    rating: null
  };

  constructor(
    private alertService: AlertService,
    private route: ActivatedRoute,
    private router: Router,
    private evaluationService: EvaluationService,
    private userService: UserService
  ) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const userToken = params.get('tokenId');
      if (userToken) {
        this.fetchRevieweeDetails(userToken);
      } else {
        console.error('No user token provided in route.');
        this.router.navigate(['/']);
      }
    });
  }

  fetchRevieweeDetails(userToken: string): void {
    this.userService.getUserByToken(userToken).subscribe({
      next: (user) => {
        this.recommendation.revieweeId = user.id;
        this.recommendation.name = user.fullName;
        this.recommendation.avatar = user.imageUrl || null;
      },
      error: (err) => {
        console.error('Error fetching user details:', err);
        this.router.navigate(['/']);
      }
    });
  }

  submitRecommendation(): void {
    this.evaluationService.submitRecommendation(this.recommendation).subscribe({
      next: () => {
        this.alertService.successAlert('Recommendation submitted successfully!', 'Success');
        this.router.navigate(['/']);
      },
      error: (err) => {
        console.error('Error submitting recommendation:', err);
        this.alertService.errorAlert('Failed to submit recommendation. Please try again.');
      }
    });
  }
}

