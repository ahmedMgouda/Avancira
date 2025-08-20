import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

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
  email: string = '';
  isSubmitting: boolean = false;
  successMessage: string = '';
  errorMessage: string = '';
  formDisabled: boolean = false;

  constructor(
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private userService: UserService,
    private router: Router
  ) {
    this.resetPasswordForm = this.fb.group({
      email: [{ value: '', disabled: true }, [Validators.required, Validators.email]],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.token = params['token'] || '';
      this.email = params['email'] || '';

      if (!this.token || !this.email) {
        this.errorMessage = 'Invalid or missing reset token or email.';
        this.formDisabled = true;
        this.resetPasswordForm.disable();
        return;
      }
      
      // Set the email value in the form
      this.resetPasswordForm.patchValue({ email: this.email });
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
      email: this.email,
      password: this.resetPasswordForm.value.newPassword,
      confirmPassword: this.resetPasswordForm.value.confirmPassword,
      token: this.token,
    };

    this.userService.resetPassword(resetData).subscribe({
      next: () => {
        this.successMessage = 'Your password has been reset successfully!';
        this.errorMessage = '';
        this.isSubmitting = false;
        this.resetPasswordForm.reset();
        setTimeout(() => {
          this.router.navigate(['/signin']);
        }, 3000);
      },
      error: (err) => {
        this.errorMessage = err.error.message || 'Failed to reset the password.';
        this.isSubmitting = false;
      },
    });
  }
}
