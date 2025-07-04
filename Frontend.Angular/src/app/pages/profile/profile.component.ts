import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { MapAddressComponent } from '../../components/map-address/map-address.component';

import { AlertService } from '../../services/alert.service';
import { AuthService } from '../../services/auth.service';
import { SpinnerService } from '../../services/spinner.service'; 
import { UserService } from '../../services/user.service';

import { ImageFallbackDirective } from '../../directives/image-fallback.directive';

import { UserDiplomaStatus } from '../../models/enums/user-diploma-status';

import { Address, User } from '../../models/user';


@Component({
  selector: 'app-profile',
  imports: [CommonModule, FormsModule, MapAddressComponent, ImageFallbackDirective],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProfileComponent implements OnInit {
  timezones: { id: string; label: string }[] = [];

  // Enums and Data Models
  DiplomaStatus = UserDiplomaStatus;

  // States
  profile: User = {} as User;

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


  // Diploma
  diplomaStatus: UserDiplomaStatus = UserDiplomaStatus.NotSubmitted;
  selectedFile: File | null = null;

  // Delete Confirmation
  deleteConfirmation = false;

  constructor(
    private alertService: AlertService,
    private authService: AuthService,
    private userService: UserService,
    private spinnerService: SpinnerService,
    private router: Router
  ) {
  }


  // 1. Lifecycle Hooks
  ngOnInit(): void {
    this.fetchDiplomaStatus();
    this.loadUserProfile();
    this.loadTimezones();
  }

  loadTimezones(): void {
    const timezones = Intl.supportedValuesOf ? Intl.supportedValuesOf('timeZone') : [
      'Australia/Sydney', 'America/New_York', 'Europe/London'
    ];
  
    this.timezones = timezones.map(tz => ({
      id: tz,
      label: this.formatTimezone(tz)
    }));
  }
  
  /**
   * Converts "America/New_York" → "New York (GMT-5:00)"
   */
  formatTimezone(timezone: string): string {
    try {
      const now = new Date();
      const formatter = new Intl.DateTimeFormat('en-US', {
        timeZone: timezone,
        timeZoneName: 'short'
      });
  
      const formatted = formatter.formatToParts(now).find(part => part.type === 'timeZoneName')?.value || timezone;
      return `${timezone.replace(/_/g, ' ')} (${formatted})`;
    } catch {
      return timezone.replace(/_/g, ' '); // Fallback
    }
  }
  
  // 2. User Profile Management
  loadUserProfile(): void {
    this.userService.getUser().subscribe({
      next: (user) => {
        this.profile = user;
        if (!this.profile.address) {
          this.profile.address = this.profile.address || {} as Address;
        }
        if (!this.profile.timeZoneId) {
          this.profile.timeZoneId = 'Australia/Sydney';
        }
      },
      error: (err) => console.error('Failed to fetch user profile', err)
    });
  }


  saveProfile(): void {
    if (!this.profile) return;
  
    this.spinnerService.show();
  
    this.userService.updateUser(this.profile).subscribe({
      next: () => {
        this.alertService.successAlert('Profile updated successfully.', 'Success');
        this.spinnerService.hide();
      },
      error: (err) => {
        console.error('Failed to update profile', err);
        this.alertService.errorAlert('Failed to update profile. Please try again.', 'Error');
        this.spinnerService.hide();
      }
    });
  }
  
  updateAddress(location: Address) {
    if (this.profile && this.profile.address) {
      this.profile.address.formattedAddress = location.formattedAddress;
      this.profile.address.latitude = location.latitude;
      this.profile.address.longitude = location.longitude;

      this.profile.address.postalCode = location.postalCode;
      this.profile.address.country = location.country;
      this.profile.address.state = location.state;
      this.profile.address.city = location.city;
      this.profile.address.streetAddress = location.streetAddress;
    }
  }

  onProfilePictureUpload(event: Event): void {
    if (!this.profile) return;

    const input = event.target as HTMLInputElement;
    if (input?.files?.length) {
      const file = input.files[0];

      const allowedTypes = ['image/png', 'image/jpeg', 'image/gif'];
      if (!allowedTypes.includes(file.type) || file.size > 2 * 1024 * 1024) {
        this.alertService.warningAlert('Image must be PNG, JPG or GIF and less than 2MB.');
        input.value = '';
        return;
      }

      const reader = new FileReader();
      reader.onload = () => {
        if (this.profile) {
          // Set the image URL on the profile object
          this.profile.imageUrl = reader.result as string;
        }

        // Send the image file to the server
        this.spinnerService.show();
        this.userService.updateUser(this.profile!, file).subscribe({
          next: () => this.spinnerService.hide(),
          error: (err) => {
            console.error('Error updating profile picture:', err);
            this.spinnerService.hide();
          }
        });
      };

      reader.readAsDataURL(file);
    }
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
        this.alertService.successAlert('Diploma submitted for review.');
        this.diplomaStatus = UserDiplomaStatus.UnderReview;
      },
      error: (err) => {
        console.error('Error submitting diploma:', err);
        this.alertService.errorAlert('Failed to submit the diploma. Please try again.');
      }
    });
  }

  // 6. Account Management
  changePassword(): void {
    if (this.profile?.email) {
      this.userService.requestPasswordReset(this.profile.email).subscribe({
        next: () => {
          this.alertService.successAlert('Password reset request has been sent to your email.');
        },
        error: (err) => {
          console.error('Error sending password reset request:', err);
          this.alertService.errorAlert('Failed to send password reset request. Please try again.');
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
  
  changePasswordData = {
    oldPassword: '',
    newPassword: '',
    confirmNewPassword: ''
  };
  changeOldPassword(): void {
    if (this.changePasswordData.newPassword !== this.changePasswordData.confirmNewPassword) {
      this.alertService.warningAlert('New password and confirmation do not match.');
      return;
    }

    this.userService.changePassword(
      this.changePasswordData.oldPassword,
      this.changePasswordData.newPassword,
      this.changePasswordData.confirmNewPassword
    ).subscribe({
      next: () => {
        this.alertService.successAlert('Password updated successfully.');
        this.changePasswordData = { oldPassword: '', newPassword: '', confirmNewPassword: '' };
      },
      error: (err) => {
        this.alertService.errorAlert('Failed to update password: ' + (err.error?.message || 'Unknown error'));
      }
    });
  }
}
