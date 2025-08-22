// src/app/auth/signin/signin.component.ts
import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FacebookService, InitParams } from 'ngx-facebook';
import { ToastrService } from 'ngx-toastr';
import { from, Observable } from 'rxjs';
import { finalize, switchMap, take, tap } from 'rxjs/operators';

import { AlertService } from '../../services/alert.service';
import { AuthService } from '../../services/auth.service';
import { ConfigService } from '../../services/config.service';
import { GoogleAuthService } from '../../services/google-auth.service';
import { SpinnerService } from '../../services/spinner.service';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-signin',
  templateUrl: './signin.component.html',
  styleUrls: ['./signin.component.scss'],
  imports: [CommonModule, ReactiveFormsModule, RouterModule]
})
export class SigninComponent implements OnInit {
  loginForm!: FormGroup;
  returnUrl = '/';

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService,
    private config: ConfigService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private alert: AlertService,
    private user: UserService,
    private google: GoogleAuthService,
    private facebook: FacebookService
  ) {}

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
      rememberMe: [false],
    });

    this.route.queryParams.subscribe((params) => {
      this.returnUrl = params['returnUrl'] || '/';
    });

    // Initialize Facebook SDK
    this.config
      .loadConfig()
      .pipe(take(1))
      .subscribe({
        next: () => {
          const params: InitParams = {
            appId: this.config.get('facebookAppId'),
            cookie: true,
            xfbml: true,
            version: 'v21.0',
          };
          this.facebook.init(params);
        },
        error: (err) => {
          console.error('Config load error:', err);
        },
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
          this.router.navigateByUrl(this.returnUrl);
        },
        error: () => {
          this.toastr.error('Invalid credentials', 'Login Failed');
          this.spinner.hide();
        },
      });
  }

  /** Google login using Identity Services */
  loginWithGoogle(): void {
    this.spinner.show();
    const clientId = this.config.get('googleClientId');

    from(this.google.init(clientId))
      .pipe(
        switchMap(() => from(this.google.signIn())),
        switchMap((idToken) => this.handleSocialLogin('google', idToken)),
        finalize(() => this.spinner.hide())
      )
      .subscribe({
        error: (err) => {
          console.error('Google login error:', err);
          this.toastr.error('Google login failed. Try again.', 'Error');
        },
      });
  }

  /** Facebook login */
  loginWithFacebook(): void {
    this.spinner.show();

    from(this.facebook.login({ scope: 'email,public_profile' }))
      .pipe(
        switchMap((res) =>
          this.handleSocialLogin('facebook', res.authResponse.accessToken)
        ),
        finalize(() => this.spinner.hide())
      )
      .subscribe({
        error: (err) => {
          console.error('Facebook login failed:', err);
          this.toastr.error('Facebook login failed.', 'Error');
        },
      });
  }

  /** Common handler for social login */
  private handleSocialLogin(
    provider: 'google' | 'facebook',
    token: string
  ): Observable<void> {
    return this.authService
      .externalLogin(provider, token)
      .pipe(tap(() => this.router.navigateByUrl(this.returnUrl)));
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
