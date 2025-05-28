import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-profile-image',
  imports: [CommonModule, FormsModule],
  templateUrl: './profile-image.component.html',
  styleUrl: './profile-image.component.scss'
})
export class ProfileImageComponent {
  @Input() profileImagePath?: string; // Profile image path
  @Input() firstName?: string;    // User's first name
  @Input() lastName?: string;     // User's last name
  @Input() sizeClass: string = 'large'; // Default size is 'medium'
  @Input() customClass?: string;     // Custom class for the <img> element
  isImageError = false;

  getInitials(): string {
    const firstInitial = this.firstName ? this.firstName.charAt(0).toUpperCase() : '';
    const lastInitial = this.lastName ? this.lastName.charAt(0).toUpperCase() : '';
    return `${firstInitial}${lastInitial}`;
  }

  get showImage(): boolean {
    // Show the image only if profileImage is defined, non-empty, and no error occurred
    return !!this.profileImagePath && !this.isImageError;
  }
}
