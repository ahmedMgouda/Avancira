import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { MapAddressComponent } from '../map-address/map-address.component';
import { ProfileImageComponent } from '../profile-image/profile-image.component';

import { AlertService } from '../../services/alert.service';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';

import { PaymentHistory } from '../../models/payment-history';
import { DiplomaStatus, PaymentSchedule, User } from '../../models/user';

@Component({
  selector: 'app-profile-details',
  imports: [CommonModule, FormsModule, MapAddressComponent, ProfileImageComponent],
  templateUrl: './profile-details.component.html',
  styleUrl: './profile-details.component.scss'
})
export class ProfileDetailsComponent implements OnInit {
  // Enums and Data Models
  DiplomaStatus = DiplomaStatus;

  // States
  profile: User | null = null;
  payment: PaymentHistory | null = null;

  // Notifications
  notifications = {
    sms: [{ label: 'Lesson Requests', enabled: true }],
    email: [
      { label: 'Account activity', enabled: true },
      { label: 'Lesson Requests', enabled: true },
      { label: 'Offers concerning my listings', enabled: false },
      { label: 'Newsletter', enabled: true }
    ],
  };

  // Payment
  paypalAccountAdded: boolean = false;
  paymentPreference: PaymentSchedule = PaymentSchedule.PerLesson;
  compensationPercentage: number = 50;

  // Diploma
  diplomaStatus: DiplomaStatus = DiplomaStatus.NotSubmitted;
  selectedFile: File | null = null;

  // Delete Confirmation
  deleteConfirmation = false;

  constructor(
    private alertService: AlertService,
    private authService: AuthService,
    private userService: UserService,
    private router: Router
  ) {
  }


  // 1. Lifecycle Hooks
  ngOnInit(): void {
    this.fetchDiplomaStatus();
    this.loadUserProfile();
  }

  // 2. User Profile Management
  loadUserProfile(): void {
    this.userService.getUser().subscribe({
      next: (user) => {
        this.profile = user
      },
      error: (err) => console.error('Failed to fetch user profile', err)
    });
  }

  saveProfile(): void {
    if (this.profile) {
      this.userService.updateUser(this.profile).subscribe({
        error: (err) => console.error('Failed to update profile', err)
      });
    }
  }

  updateAddress(location: { address: string; lat: number; lng: number }) {
    if (this.profile) {
      this.profile.address = location.address;
      this.profile.latitude = location.lat;
      this.profile.longitude = location.lng;
    }
  }

  onProfilePictureUpload(): void {
    if (!this.profile) return;

    const fileInput = document.createElement('input');
    fileInput.type = 'file';
    fileInput.accept = 'image/*';
    fileInput.onchange = (event: any) => {
      const file = event.target.files[0];
      if (file) {
        const reader = new FileReader();
        reader.onload = () => {
          if (this.profile) this.profile.imageUrl = reader.result as string;

          this.userService.updateUser(this.profile!, file).subscribe({
            error: (err) => console.error('Error updating profile picture:', err)
          });
        };
        reader.readAsDataURL(file);
      }
    };
    fileInput.click();
  }

  // 4. Diploma Management
  fetchDiplomaStatus(): void {
    this.userService.getDiplomaStatus().subscribe({
      next: (data) => this.diplomaStatus = data.status,
      error: (err) => console.error('Error fetching diploma status:', err)
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
    }
  }

  submitDiploma(): void {
    if (!this.selectedFile) {
      this.alertService.warningAlert('Please select a diploma file before submitting.');
      return;
    }
    this.userService.submitDiploma(this.selectedFile).subscribe({
      next: () => {
        this.alertService.successAlert('Diploma submitted for review.', 'Success');
        this.diplomaStatus = DiplomaStatus.UnderReview;
      },
      error: (err) => {
        console.error('Error submitting diploma:', err);
        this.alertService.errorAlert('Failed to submit the diploma. Please try again.', 'Error');
      }
    });
  }

  // 6. Account Management
  changePassword(): void {
    if (this.profile?.email) {
      this.userService.requestPasswordReset(this.profile.email).subscribe({
        next: () => {
          this.alertService.successAlert('Password reset request has been sent to your email.', 'Success');
        },
        error: (err) => {
          console.error('Error sending password reset request:', err);
          this.alertService.errorAlert('Failed to send password reset request. Please try again.', 'Error');
        }
      });
    }
  }

  async confirmAndDeleteAccount(): Promise<void> {
    if (!this.deleteConfirmation) return;

    const confirmed = await this.alertService.confirm(
      'Are you sure you want to delete your account? This action is irreversible.',
      'Delete Account',
      'Yes, delete my account'
    );

    if (!confirmed) return;

    this.userService.deleteAccount().subscribe({
      next: () => {
        this.alertService.successAlert('Your account has been deleted.', 'Account Deleted');
        this.authService.logout();
        this.router.navigate(['/goodbye']);
      },
      error: (err) => {
        console.error('Failed to delete account:', err);
        this.alertService.errorAlert('Failed to delete account. Please try again.', 'Error');
      },
    });
  }
}
