// src/app/auth/signin/signin.component.ts
import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FacebookService, InitParams } from 'ngx-facebook';
import { ToastrService } from 'ngx-toastr';

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
    });

    this.route.queryParams.subscribe((params) => {
      this.returnUrl = params['returnUrl'] || '/';
    });

    // Initialize Facebook SDK
    this.config.loadConfig().subscribe({
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

    const { email, password } = this.loginForm.value;
    this.spinner.show();

    this.authService.login(email, password).subscribe({
      next: () => {
        this.router.navigateByUrl(this.returnUrl);
      },
      error: () => this.toastr.error('Invalid credentials', 'Login Failed'),
      complete: () => this.spinner.hide(),
    });
  }

  /** Google login using Identity Services */
  async loginWithGoogle(): Promise<void> {
    try {
      this.spinner.show();
      const clientId = this.config.get('googleClientId');
      await this.google.init(clientId);
      const idToken = await this.google.signIn();
      await this.handleSocialLogin('google', idToken);
    } catch (err) {
      console.error('Google login error:', err);
      this.toastr.error('Google login failed. Try again.', 'Error');
    } finally {
      this.spinner.hide();
    }
  }

  /** Facebook login */
  loginWithFacebook(): void {
    this.spinner.show();
    this.facebook
      .login({ scope: 'email,public_profile' })
      .then(async (res) => {
        const token = res.authResponse.accessToken;
        await this.handleSocialLogin('facebook', token);
      })
      .catch((err) => {
        console.error('Facebook login failed:', err);
        this.toastr.error('Facebook login failed.', 'Error');
        this.spinner.hide();
      });
  }

  /** Common handler for social login */
  handleSocialLogin(provider: string, token: string): void {
    // this.authService.socialLogin(provider, token).subscribe({
    //   next: () => {
    //     this.router.navigateByUrl(this.returnUrl);
    //   },
    //   error: (err : any) => {
    //     console.error(`${provider} login error:`, err);
    //     this.toastr.error(`${provider} login failed.`, 'Error');
    //   },
    // });
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
      this.user.requestPasswordReset(email).subscribe();
      this.alert.successAlert(
        'Check your inbox',
        'We’ve sent a reset link to your email.',
        'Return to login'
      );
    }
  }
}
