// src/app/auth/signup/signup.component.ts
import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { RouterModule } from '@angular/router'; 
import { FacebookService, InitParams } from 'ngx-facebook';
import { ToastrService } from 'ngx-toastr';

import { AuthService } from '../../services/auth.service';
import { ConfigService } from '../../services/config.service';
import { GoogleAuthService } from '../../services/google-auth.service';
import { SpinnerService } from '../../services/spinner.service';
import { ValidatorService } from '../../validators/password-validator.service';
import { passwordComplexityValidator } from '../../validators/password.validators';
import { RegisterUserRequest } from '../../models/register-user-request';

@Component({
  selector: 'app-signup',
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.scss'],
  imports: [CommonModule, ReactiveFormsModule, RouterModule]
})
export class SignupComponent implements OnInit {
  signupForm!: FormGroup;
  signupError = '';
  isSubmitting = false;
  referralToken: string | null = null;
  returnUrl = '/';

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService,
    private config: ConfigService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private google: GoogleAuthService,
    private facebook: FacebookService
  ) {}

  ngOnInit(): void {
    this.signupForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      userName: ['', Validators.required],
      phoneNumber: [''],
      timeZoneId: [''],
      email: ['', [Validators.required, Validators.email]],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          passwordComplexityValidator(),
        ],
      ],
      verifyPassword: ['', [Validators.required, ValidatorService.matchesPassword('password')]],
      agreeToTerms: [false, Validators.requiredTrue],
    });

    const timeZoneId = Intl.DateTimeFormat().resolvedOptions().timeZone;
    this.signupForm.patchValue({ timeZoneId });

    this.route.queryParams.subscribe((params) => {
      this.referralToken = params['referral'] || null;
      this.returnUrl = params['returnUrl'] || '/';
    });

    // Init Facebook SDK
    this.config.loadConfig().subscribe({
      next: () => {
        const initParams: InitParams = {
          appId: this.config.get('facebookAppId'),
          cookie: true,
          xfbml: true,
          version: 'v21.0',
        };
        this.facebook.init(initParams);
      },
      error: (err) => console.error('Facebook SDK init failed:', err),
    });
  }

  /** Handle manual signup */
  onSignup(): void {
    if (this.signupForm.invalid) {
      this.signupForm.markAllAsTouched();
      return;
    }
    const { verifyPassword, ...rest } = this.signupForm.value;
    const payload: RegisterUserRequest = {
      ...rest,
      confirmPassword: verifyPassword,
      phoneNumber: rest.phoneNumber || undefined,
      timeZoneId: rest.timeZoneId || undefined,
      referralToken: this.referralToken ?? undefined,
    };
    this.signupError = '';
    this.isSubmitting = true;
    this.spinner.show();

    this.authService
      .register(payload)
      .subscribe({
        next: () => this.loginUser(),
        error: (err) => {
          const errors = err?.error?.errors;
          if (errors) {
            this.signupError = Object.values(errors).flat().join(' ');
          } else {
            this.signupError =
              err?.error?.message || err?.error?.title || err?.message || 'Signup failed. Please try again.';
          }
          this.spinner.hide();
          this.isSubmitting = false;
        },
      });
  }

  /** Attempt login after signup */
  loginUser(): void {
    const { email, password } = this.signupForm.value;
    this.authService.login(email, password).subscribe({
      next: (res) => {
        if (res) {
          this.router.navigate(['/complete-registration']);
        } else {
          this.signupError = 'Signup succeeded, but login failed.';
        }
      },
      error: () => {
        this.signupError = 'Signup succeeded, but automatic login failed.';
      },
      complete: () => {
        this.spinner.hide();
        this.isSubmitting = false;
      },
    });
  }

  /** Facebook signup */
  signupWithFacebook(): void {
    this.spinner.show();
    this.facebook
      .login({ scope: 'email,public_profile' })
      .then(async (res) => {
        const token = res.authResponse.accessToken;
        await this.handleSocialSignup('facebook', token);
      })
      .catch((err) => {
        console.error('Facebook signup error:', err);
        this.signupError = 'Facebook signup failed.';
        this.spinner.hide();
      });
  }

  /** Google signup with GIS */
  async signupWithGoogle(): Promise<void> {
    try {
      this.spinner.show();
      const clientId = this.config.get('googleClientId');
      await this.google.init(clientId);
      const idToken = await this.google.signIn();
      await this.handleSocialSignup('google', idToken);
    } catch (err) {
      console.error('Google signup error:', err);
      this.signupError = 'Google signup failed. Try again.';
    } finally {
      this.spinner.hide();
    }
  }

  /** Handle token verification with backend */
  handleSocialSignup(provider: string, token: string): void {
    // this.authService.socialLogin(provider, token).subscribe({
    //   next: (res) => {
    //     if (res) {
    //       this.router.navigateByUrl(this.returnUrl);
    //     } else {
    //       this.router.navigate(['/complete-registration']);
    //     }
    //   },
    //   error: (err) => {
    //     console.error(`${provider} signup error:`, err);
    //     this.signupError = `${provider} signup failed.`;
    //   },
    // });
  }
}
