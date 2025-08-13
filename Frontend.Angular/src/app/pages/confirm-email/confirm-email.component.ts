import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil, timer } from 'rxjs';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-confirm-email',
  imports: [CommonModule, FormsModule],
  templateUrl: './confirm-email.component.html',
  styleUrl: './confirm-email.component.scss'
})
export class ConfirmEmailComponent {
  isLoading = true;
  verificationSuccess = false;
  verificationError = false;
  errorMessage: string = '';
  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const userId = params['userId'];
      const token = params['token'];

      if (userId && token) {
        this.verifyEmail(userId, token);
      } else {
        this.isLoading = false;
        this.verificationError = true;
        this.errorMessage = "Invalid confirmation link.";
      }
    });
  }

  verifyEmail(userId: string, token: string): void {
    // this.authService.confirmEmail(userId, token).pipe(takeUntil(this.destroy$)).subscribe({
    //   next: (response: any) => {
    //     if (response && response.success) { // Check if the response is successful
    //       this.verificationSuccess = true;
    //       timer(5000).pipe(takeUntil(this.destroy$)).subscribe(() => {
    //         this.router.navigate(['/dashboard']);
    //       });
    //     } else {
    //       this.verificationError = true;
    //       this.errorMessage = response?.message || "Email verification failed.";
    //     }
    //     this.isLoading = false;
    //   },
    //   error: (error: HttpErrorResponse) => {
    //     this.isLoading = false;
    //     this.verificationError = true;
    //     this.errorMessage = error.error?.message || "An error occurred during email verification.";
    //     console.error("Email verification error:", error); // Log the full error
    //   }
    // });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
