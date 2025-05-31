import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FacebookService, InitParams, LoginResponse } from 'ngx-facebook';

import { AuthService } from '../../services/auth.service';
import { ConfigService } from '../../services/config.service';
import { GoogleAuthService } from '../../services/google-auth.service';
import { ValidatorService } from '../../validators/password-validator.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-signup',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.scss'],
})
export class SignupComponent implements OnInit {
  signupForm!: FormGroup;
  signupError = '';
  referralToken: string | null = null;
  returnUrl: string = '/';

  constructor(
    private fb: FacebookService,
    private form: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    private configService: ConfigService,
    private googleAuthService: GoogleAuthService
  ) { }

  ngOnInit(): void {
    // Initialize the form
    this.signupForm = this.form.group({
      email: [
        '',
        [Validators.required, Validators.email],
      ],
      password: ['', [
        Validators.required,
        Validators.minLength(6),
        ValidatorService.hasNonAlphanumeric(),
        ValidatorService.hasLowercase(),
        ValidatorService.hasUppercase(),
      ]],
      verifyPassword: ['', [
        Validators.required,
        ValidatorService.matchesPassword('password')
      ]],
    });

    // Listen to query parameters
    this.route.queryParams.subscribe((params) => {
      this.referralToken = params['referral'] || null;
      this.returnUrl = params['returnUrl'] || '/';
    });


    // Initialize Google Authentication
    this.configService.loadConfig().subscribe({
      next: async () => {

        const initParams: InitParams = {
          appId: this.configService.get('facebookAppId'),
          cookie: true,
          xfbml: true,
          version: 'v21.0',
        };
        this.fb.init(initParams);

        await this.googleAuthService.init(this.configService.get('googleClientId'));
      },
      error: (err) => {
        console.error('Failed to load configuration:', err.message);
      },
    });
  }

  // Handle form submission
  onSignup(): void {
    if (this.signupForm.invalid) {
      return;
    }

    this.authService
      .register(
        this.signupForm.value.email,
        this.signupForm.value.password,
        this.signupForm.value.verifyPassword,
        this.referralToken
      )
      .subscribe({
        next: (errorMessage: string | null) => {
          if (!errorMessage) {
            this.loginUser(); // Continue with login on successful registration
          } else {
            this.signupError = errorMessage; // Handle registration error
          }
        },
        error: (error: any) => {
          this.signupError =
            error?.message || 'An unexpected error occurred. Please try again.';
        },
      });
  }

  // Handle user login after successful signup
  loginUser(): void {
    this.authService.login(this.signupForm.value.email, this.signupForm.value.password).subscribe({
      next: () => {
        this.router.navigate(['/complete-registration']);
      },
      error: () => {
        this.signupError = 'Signup succeeded, but automatic login failed.';
      },
    });
  }

  /** ✅ Facebook Login */
  signupWithFacebook(): void {
    this.fb
      .login({ scope: 'email,public_profile' })
      .then(async (response: LoginResponse) => {
        const accessToken = response.authResponse.accessToken;
        console.log('✅ Facebook Login Success:', accessToken);
        await this.handleSocialSignup('facebook', accessToken);
      })
      .catch((error) => {
        console.error('❌ Facebook login error:', error);
      });
  }

  /** ✅ Google Signup */
  async signupWithGoogle(): Promise<void> {
    try {
      await this.googleAuthService.init(this.configService.get('googleClientId'));
      const auth2 = this.googleAuthService.getAuthInstance();
      if (!auth2) throw new Error('Google Auth instance not initialized');

      const googleUser = await auth2.signIn();
      const idToken = googleUser.getAuthResponse().id_token;
      console.log('✅ Google Signup Success:', idToken);

      await this.handleSocialSignup('google', idToken);
    } catch (error) {
      console.error('❌ Google Signup Failed:', error);
      this.signupError = 'Google signup failed. Please try again.';
    }
  }

  /** ✅ Handle Social Signup */
  private async handleSocialSignup(
    provider: string,
    token: string
  ): Promise<void> {
    try {
      const result = await firstValueFrom(
        this.authService.socialLogin(provider, token)
      );
      this.authService.saveToken(result.token);
      this.authService.saveRoles(result.roles);

      if (result.isRegistered) {
        this.router.navigateByUrl(this.returnUrl);
      } else {
        this.router.navigate(['/complete-registration']);
      }
    } catch (error: any) {
      console.error(`❌ ${provider} signup verification failed:`, error.message);
      this.signupError = `Failed to sign up with ${provider}.`;
    }
  }
}
