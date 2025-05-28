import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-complete-registration',
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './complete-registration.component.html',
  styleUrl: './complete-registration.component.scss'
})
export class CompleteRegistrationComponent implements OnInit, OnDestroy {
  profileForm!: FormGroup;
  isLoading = true;
  errorMessage: string | null = null;
  registrationCompleted = false;
  private destroy$ = new Subject<void>();

  constructor(
    private userService: UserService,
    private router: Router,
    private fb: FormBuilder
  ) {}

  async ngOnInit() {
    this.profileForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      dateOfBirth: [''],
      phoneNumber: ['', Validators.required],
      skypeId: [''],
      hangoutId: [''],
    });
  }

  saveProfile() {
    if (this.profileForm.invalid) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;

    this.userService.updateUser(this.profileForm.value).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.registrationCompleted = true;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.errorMessage = err.message || "Failed to save profile.";
        console.error('Failed to update profile', err);
        this.isLoading = false;
      },
      complete: () => {
        this.isLoading = false;
      },
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
