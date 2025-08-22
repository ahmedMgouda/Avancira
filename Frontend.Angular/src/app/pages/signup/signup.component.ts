// src/app/auth/signup/signup.component.ts
import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  Validators,
} from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Subject, from, takeUntil } from 'rxjs';
import { finalize, switchMap } from 'rxjs/operators';

import { AuthService } from '../../services/auth.service';
import { SocialAuthService } from '../../services/social-auth.service';
import { SpinnerService } from '../../services/spinner.service';
import { ValidatorService } from '../../validators/password-validator.service';
import { passwordComplexityValidator } from '../../validators/password.validators';
import { MIN_PASSWORD_LENGTH } from '../../validators/password-rules';
import { RegisterUserRequest } from '../../models/register-user-request';
import { FacebookAuthService } from '../../services/facebook-auth.service';

interface SignupForm {
  firstName: FormControl<string>;
  lastName: FormControl<string>;
  userName: FormControl<string>;
  phoneNumber: FormControl<string>;
  timeZoneId: FormControl<string>;
  email: FormControl<string>;
  password: FormControl<string>;
  verifyPassword: FormControl<string>;
  acceptTerms: FormControl<boolean>;
}

@Component({
  selector: 'app-signup',
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.scss'],
  imports: [CommonModule, ReactiveFormsModule, RouterModule]
})
export class SignupComponent implements OnInit, OnDestroy {
  signupForm: FormGroup<SignupForm>;
  signupError = '';
  isSubmitting = false;
  referralToken: string | null = null;
  returnUrl = '/';
  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService,
    private spinner: SpinnerService,
    private toastr: ToastrService,
    private socialAuth: SocialAuthService,
    private facebookAuth: FacebookAuthService
  ) {
    this.signupForm = this.fb.nonNullable.group<SignupForm>({
      firstName: this.fb.nonNullable.control('', Validators.required),
      lastName: this.fb.nonNullable.control('', Validators.required),
      userName: this.fb.nonNullable.control('', Validators.required),
      phoneNumber: this.fb.nonNullable.control(''),
      timeZoneId: this.fb.nonNullable.control(''),
      email: this.fb.nonNullable.control('', [
        Validators.required,
        Validators.email,
      ]),
      password: this.fb.nonNullable.control('', [
        Validators.required,
        Validators.minLength(MIN_PASSWORD_LENGTH),
        passwordComplexityValidator(),
      ]),
      verifyPassword: this.fb.nonNullable.control('', [
        Validators.required,
        ValidatorService.matchesPassword('password'),
      ]),
      acceptTerms: this.fb.nonNullable.control(false, Validators.requiredTrue),
    });
  }

  ngOnInit(): void {
    const timeZoneId = Intl.DateTimeFormat().resolvedOptions().timeZone;
    this.signupForm.patchValue({ timeZoneId });

    this.route.queryParams
      .pipe(takeUntil(this.destroy$))
      .subscribe((params) => {
        this.referralToken = params['referral'] || null;
        this.returnUrl = params['returnUrl'] || '/';
      });
  }

  /** Handle manual signup */
  onSignup(): void {
    if (this.signupForm.invalid) {
      this.signupForm.markAllAsTouched();
      return;
    }
    const { verifyPassword, ...rest } = this.signupForm.getRawValue();
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
      .pipe(
        finalize(() => {
          this.spinner.hide();
          this.isSubmitting = false;
        })
      )
      .subscribe({
        next: () => {
          this.toastr.success(
            'Registration successful! Please verify your email before signing in.'
          );
          this.router.navigate(['/check-email']);
        },
        error: (err) => {
          const errors = err?.error?.errors;
          if (errors) {
            this.signupError = Object.values(errors).flat().join(' ');
          } else {
            this.signupError =
              err?.error?.message || err?.error?.title || err?.message || 'Signup failed. Please try again.';
          }
        },
      });
  }

  authenticate(provider: 'google' | 'facebook'): void {
    this.spinner.show();
    const auth$ =
      provider === 'facebook'
        ? from(this.facebookAuth.ensureInitialized()).pipe(
            switchMap(() => this.socialAuth.authenticate(provider))
          )
        : this.socialAuth.authenticate(provider);

    auth$
      .pipe(finalize(() => this.spinner.hide()))
      .subscribe({
        next: () => {
          this.router.navigateByUrl(this.returnUrl);
        },
        error: (err) => {
          console.error(`${provider} signup error:`, err);
          this.toastr.error(`${provider} signup failed.`, 'Error');
        },
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
