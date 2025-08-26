// src/app/auth/signin/signin.component.ts
import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';

import { AlertService } from '../../services/alert.service';
import { AuthService } from '../../services/auth.service';
import { ConfigService } from '../../services/config.service';
import { UserService } from '../../services/user.service';
import { SocialLoginButtonsComponent } from '../../components/social-login-buttons/social-login-buttons.component';
import { SocialProvider } from '../../models/social-provider';

@Component({
  selector: 'app-signin',
  templateUrl: './signin.component.html',
  styleUrls: ['./signin.component.scss'],
  imports: [CommonModule, ReactiveFormsModule, RouterModule, SocialLoginButtonsComponent]
})
export class SigninComponent implements OnInit {
  loginForm!: FormGroup;
  returnUrl = '/';
  readonly Provider = SocialProvider;
  enabledProviders: SocialProvider[] = [];

  /**
   * Validates and sanitizes a return URL.
   * Ensures the URL is a relative path within the app.
   * Defaults to '/' if validation fails.
   */
  private sanitizeReturnUrl(url?: string): string {
    if (!url) return '/';
    const isRelative = /^\/(?!\/)/.test(url) && !url.includes('://');
    return isRelative ? url : '/';
  }

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService,
    private config: ConfigService,
    private toastr: ToastrService,
    private alert: AlertService,
    private user: UserService
  ) {}

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
    });

    this.route.queryParams.subscribe((params) => {
      this.returnUrl = this.sanitizeReturnUrl(params['returnUrl']);
    });
    this.config.loadConfig().subscribe({
      next: () => {
        this.enabledProviders = this.config.getEnabledSocialProviders();
      },
      error: (err) => {
        console.error('Failed to load configuration:', err);
        this.toastr.error(
          'Some features may be unavailable. Please refresh the page or try again later.',
          'Configuration Error'
        );
        this.enabledProviders = [];
      },
    });
  }

  /** Regular login */
  onLogin(): void {
    void this.authService.startLogin(this.returnUrl);
  }

  authenticate(provider: SocialProvider): void {
    void this.authService.startLogin(this.returnUrl, provider);
  }

  /** Password reset prompt */
  async resetPassword(): Promise<void> {
    const email = await this.alert.promptForInput(
      'Reset Password',
      'Enter your email to reset your password.',
      'email',
      'Enter email',
      'Send'
    );

    if (email) {
      this.user.requestPasswordReset(email).subscribe({
        next: () =>
          this.alert.successAlert(
            'Check your inbox',
            'We’ve sent a reset link to your email.',
            'Return to login'
          ),
        error: (err) => {
          console.error('Password reset request failed:', err);
          this.toastr.error(
            'Failed to send reset link. Please try again.',
            'Error'
          );
        },
      });
    }
  }
}
