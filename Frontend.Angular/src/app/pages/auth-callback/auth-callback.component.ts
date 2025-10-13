import { CommonModule } from '@angular/common';
import { Component, inject,OnInit } from '@angular/core';
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
console.log('ON CALLBACK:');
  console.log('State from sessionStorage:', sessionStorage.getItem('auth:state'));
  console.log('Code verifier from sessionStorage:', sessionStorage.getItem('auth:code_verifier'));
  console.log('Full sessionStorage:', { ...sessionStorage });

    this.handleOAuthCallback();
  }

  /**
   * Handle OAuth2 authorization code callback
   * 1. Extract code and state from URL query params
   * 2. Validate state (CSRF protection)
   * 3. Exchange code for tokens
   * 4. Load user profile and permissions
   * 5. Redirect to saved return URL or dashboard
   */
  private handleOAuthCallback(): void {
    // Extract query parameters from redirect URL
    const code = this.route.snapshot.queryParams['code'];
    const state = this.route.snapshot.queryParams['state'];
    const errorParam = this.route.snapshot.queryParams['error'];
    const errorDescription = this.route.snapshot.queryParams['error_description'];

    // Handle OAuth error response from backend
    if (errorParam) {
      this.error = errorDescription 
        ? `OAuth Error: ${errorParam} - ${errorDescription}`
        : `OAuth Error: ${errorParam}`;
      this.isProcessing = false;
      console.error('OAuth callback error:', { errorParam, errorDescription });
      return;
    }

    // Validate required parameters
    if (!code) {
      this.error = 'Missing authorization code from OAuth provider';
      this.isProcessing = false;
      console.error('Missing code in OAuth callback');
      return;
    }

    if (!state) {
      this.error = 'Missing state parameter - possible CSRF attack';
      this.isProcessing = false;
      console.error('Missing state in OAuth callback');
      return;
    }

    // Exchange authorization code for tokens
    // This will:
    // - Validate the state matches what we sent
    // - POST to /connect/token with code + code_verifier
    // - Store access_token and refresh_token
    // - Load user profile from /connect/userinfo
    // - Load user permissions from /api/users/permissions
    // - Load user profile details from /api/users/profile
    this.authService.handleAuthCallback(code, state).subscribe({
      next: () => {
        // Success - redirect to dashboard or saved return URL
        const returnUrl = sessionStorage.getItem('auth:return_url') ?? '/dashboard';
        sessionStorage.removeItem('auth:return_url');
        
        console.info('OAuth callback successful, redirecting to:', returnUrl);
        void this.router.navigateByUrl(returnUrl);
      },
      error: (err) => {
        // Authentication failed
        this.error = err.message ?? 'Failed to complete authentication';
        this.isProcessing = false;
        
        console.error('OAuth callback error:', err);
      },
    });
  }

  goToLogin(): void {
    void this.router.navigate(['/signin']);
  }
}