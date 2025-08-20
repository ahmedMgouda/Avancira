import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-forgot-password',
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent implements OnInit, OnDestroy {
  forgotPasswordForm: FormGroup;
  successMessage = '';
  errorMessage = '';
  isSubmitting = false;
  cooldown = 0;
  private cooldownInterval?: ReturnType<typeof setInterval>;

  constructor(private fb: FormBuilder, private userService: UserService) {
    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  ngOnInit(): void {
    this.cooldown = this.userService.getRequestPasswordResetCooldown();
    if (this.cooldown > 0) {
      this.startCooldown();
    }
  }

  private startCooldown(): void {
    this.forgotPasswordForm.disable();
    this.cooldownInterval = setInterval(() => {
      this.cooldown--;
      if (this.cooldown <= 0) {
        this.forgotPasswordForm.enable();
        clearInterval(this.cooldownInterval);
      }
    }, 1000);
  }

  async onSubmit(): Promise<void> {
    if (this.forgotPasswordForm.invalid || this.cooldown > 0) {
      return;
    }

    this.isSubmitting = true;
    const email = this.forgotPasswordForm.value.email;

    try {
      await firstValueFrom(this.userService.requestPasswordReset(email));
      this.successMessage = 'A password reset link has been sent to your email.';
      this.errorMessage = '';
    } catch (err: any) {
      this.errorMessage = err?.error?.message || 'Unable to process password reset request.';
      this.successMessage = '';
    } finally {
      this.isSubmitting = false;
      this.cooldown = this.userService.getRequestPasswordResetCooldown();
      if (this.cooldown > 0) {
        this.startCooldown();
      }
    }
  }

  ngOnDestroy(): void {
    if (this.cooldownInterval) {
      clearInterval(this.cooldownInterval);
    }
  }
}

