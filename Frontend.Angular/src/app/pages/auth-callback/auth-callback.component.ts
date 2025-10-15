import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-auth-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="auth-callback-container">
      @if (isProcessing) {
        <div class="loading">
          <p>Processing login...</p>
          <div class="spinner"></div>
        </div>
      } @else if (error) {
        <div class="error-message">
          <h2>Authentication Failed</h2>
          <p>{{ error }}</p>
          <button (click)="goToLogin()" class="btn">
            Return to Sign In
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .auth-callback-container {
      display: flex;
      align-items: center;
      justify-content: center;
      height: 100vh;
      background: #f5f5f5;
    }

    .loading {
      text-align: center;
    }

    .spinner {
      border: 4px solid #f3f3f3;
      border-top: 4px solid #3498db;
      border-radius: 50%;
      width: 40px;
      height: 40px;
      animation: spin 1s linear infinite;
      margin: 20px auto;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .error-message {
      background: white;
      padding: 30px;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
      max-width: 400px;
      text-align: center;
    }

    .error-message h2 {
      color: #e74c3c;
      margin-top: 0;
    }

    .btn {
      background: #3498db;
      color: white;
      border: none;
      padding: 10px 20px;
      border-radius: 4px;
      cursor: pointer;
      margin-top: 15px;
    }

    .btn:hover {
      background: #2980b9;
    }
  `]
})
export class AuthCallbackComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  isProcessing = true;
  error: string | null = null;

  ngOnInit(): void {
    this.handleBffCallback();
  }

  private handleBffCallback(): void {
    const errorParam = this.route.snapshot.queryParams['error'];
    const errorDescription = this.route.snapshot.queryParams['error_description'];
    const target = this.route.snapshot.queryParams['returnUrl'];

    if (errorParam) {
      this.error = errorDescription
        ? `${errorParam}: ${errorDescription}`
        : errorParam;
      this.isProcessing = false;
      return;
    }

    this.authService.handleAuthCallback(target).subscribe({
      next: () => {
        this.isProcessing = false;
      },
      error: (err) => {
        this.error = err?.message ?? 'Failed to complete authentication';
        this.isProcessing = false;
      },
    });
  }

  goToLogin(): void {
    void this.router.navigate(['/signin']);
  }
}