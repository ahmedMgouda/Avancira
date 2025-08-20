import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs/operators';

import { UserService } from '../../services/user.service';
import { ResetPasswordRequest } from '../../models/reset-password-request';

@Component({
  selector: 'app-reset-password',
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss'
})
export class ResetPasswordComponent implements OnInit {
  resetPasswordForm: FormGroup;
  token: string = '';
  userId: string = '';
  isSubmitting: boolean = false;
  successMessage: string = '';
  errorMessage: string = '';
  formDisabled: boolean = false;

  // Password complexity patterns mirroring backend requirements
  uppercasePattern = /[A-Z]/;
  lowercasePattern = /[a-z]/;
  digitPattern = /[0-9]/;
  symbolPattern = /[^a-zA-Z0-9]/;

  constructor(
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private userService: UserService,
    private router: Router
  ) {
    this.resetPasswordForm = this.fb.group({
      newPassword: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.pattern(this.uppercasePattern),
          Validators.pattern(this.lowercasePattern),
          Validators.pattern(this.digitPattern),
          Validators.pattern(this.symbolPattern),
        ],
      ],
      confirmPassword: ['', [Validators.required]],
    });
  }

  get newPasswordControl() {
    return this.resetPasswordForm.get('newPassword');
  }

  get confirmPasswordControl() {
    return this.resetPasswordForm.get('confirmPassword');
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.token = params['token'] || '';
      this.userId = params['userId'] || '';

      if (!this.token || !this.userId) {
        this.errorMessage = 'Invalid or missing reset token or user identifier.';
        this.formDisabled = true;
        this.resetPasswordForm.disable();
        return;
      }
    });
  }

  resetPassword(): void {
    if (this.resetPasswordForm.invalid) {
      this.errorMessage = 'Please ensure the form is valid.';
      return;
    }

    if (this.resetPasswordForm.value.newPassword !== this.resetPasswordForm.value.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    this.isSubmitting = true;
    const resetData: ResetPasswordRequest = {
      userId: this.userId,
      password: this.resetPasswordForm.value.newPassword,
      confirmPassword: this.resetPasswordForm.value.confirmPassword,
      token: this.token,
    };

    this.userService
      .resetPassword(resetData)
      .pipe(finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: () => {
          this.successMessage = 'Your password has been reset successfully!';
          this.errorMessage = '';
          this.resetPasswordForm.reset();
          setTimeout(() => {
            this.router.navigate(['/signin']);
          }, 3000);
        },
        error: (err) => {
          this.errorMessage = err.error.message || 'Failed to reset the password.';
        },
      });
  }
}
