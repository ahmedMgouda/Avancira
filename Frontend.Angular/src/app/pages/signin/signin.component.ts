// src/app/auth/signin/signin.component.ts
import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { finalize } from 'rxjs/operators';

import { AlertService } from '../../services/alert.service';
import { AuthService } from '../../services/auth.service';
import { SocialAuthService } from '../../services/social-auth.service';
import { SpinnerService } from '../../services/spinner.service';
import { UserService } from '../../services/user.service';
import { SocialLoginButtonsComponent } from '../../components/social-login-buttons/social-login-buttons.component';
import { FACEBOOK as FACEBOOK_PROVIDER, GOOGLE as GOOGLE_PROVIDER, SocialProvider } from '../../models/social-provider';

@Component({
  selector: 'app-signin',
  templateUrl: './signin.component.html',
  styleUrls: ['./signin.component.scss'],
  imports: [CommonModule, ReactiveFormsModule, RouterModule, SocialLoginButtonsComponent]
})
export class SigninComponent implements OnInit {
  loginForm!: FormGroup;
  returnUrl = '/';
  readonly GOOGLE = GOOGLE_PROVIDER;
  readonly FACEBOOK = FACEBOOK_PROVIDER;

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
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private alert: AlertService,
    private user: UserService,
    private socialAuth: SocialAuthService
  ) {}

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
      rememberMe: [false],
    });

    this.route.queryParams.subscribe((params) => {
      this.returnUrl = this.sanitizeReturnUrl(params['returnUrl']);
    });

  }

  /** Regular login */
  onLogin(): void {
    if (this.loginForm.invalid) return;

    const { email, password, rememberMe } = this.loginForm.value;
    this.spinner.show();

    this.authService
      .login(email, password, rememberMe)
      .pipe(finalize(() => this.spinner.hide()))
      .subscribe({
        next: () => {
          this.router.navigateByUrl(this.sanitizeReturnUrl(this.returnUrl));
        },
        error: () => {
          this.toastr.error('Invalid credentials', 'Login Failed');
        },
      });
  }

  authenticate(provider: SocialProvider): void {
    this.spinner.show();
    this.socialAuth
      .authenticate(provider)
      .pipe(finalize(() => this.spinner.hide()))
      .subscribe({
        next: () =>
          this.router.navigateByUrl(this.sanitizeReturnUrl(this.returnUrl)),
        error: (err) => {
          console.error(`${provider} login failed:`, err);
          this.toastr.error(`${provider} login failed.`, 'Error');
        },
      });
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
