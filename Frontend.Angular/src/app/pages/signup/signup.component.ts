import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { LoginResponse } from 'ngx-facebook';

import { AuthService } from '../../services/auth.service';
import { SpinnerService } from '../../services/spinner.service';
import { SocialAuthService } from '../../services/social-auth.service';
import { ValidatorService } from '../../validators/password-validator.service';
import { firstValueFrom } from 'rxjs';
import { finalize } from 'rxjs/operators';

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
  showPassword = false;
  showVerifyPassword = false;

  constructor(
    private socialAuth: SocialAuthService,
    private form: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    private spinner: SpinnerService
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


    // Initialize external auth providers (Facebook/Google)
    this.socialAuth.init();
  }

  // Handle form submission
  onSignup(): void {
    if (this.signupForm.invalid) {
      return;
    }
    this.spinner.show();
    this.authService
      .register(
        this.signupForm.value.email,
        this.signupForm.value.password,
        this.signupForm.value.verifyPassword,
        this.referralToken
      )
      .pipe(finalize(() => this.spinner.hide()))
      .subscribe({
        next: (errorMessage: string | null) => {
          if (!errorMessage) {
            this.loginUser();
          } else {
            this.signupError = errorMessage;
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

  togglePasswordVisibility(field: 'password' | 'verifyPassword'): void {
    if (field === 'password') {
      this.showPassword = !this.showPassword;
    } else {
      this.showVerifyPassword = !this.showVerifyPassword;
    }
  }

  /** ✅ Facebook Login */
  signupWithFacebook(): void {
    this.spinner.show();
    this.socialAuth
      .loginWithFacebook()
      .then(token => this.handleSocialSignup('facebook', token))
      .catch(error => {
        console.error('❌ Facebook login error:', error);
        this.signupError = 'Facebook signup failed.';
      })
      .finally(() => this.spinner.hide());
  }

  /** ✅ Google Signup */
  async signupWithGoogle(): Promise<void> {
    try {
      this.spinner.show();
      const token = await this.socialAuth.loginWithGoogle();
      await this.handleSocialSignup('google', token);
    } catch (error) {
      console.error('❌ Google Signup Failed:', error);
      this.signupError = 'Google signup failed. Please try again.';
    } finally {
      this.spinner.hide();
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
