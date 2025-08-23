import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { SocialProvider } from '../../models/social-provider';

@Component({
  selector: 'app-social-login-buttons',
  imports: [CommonModule],
  templateUrl: './social-login-buttons.component.html',
  styleUrl: './social-login-buttons.component.scss'
})
export class SocialLoginButtonsComponent {
  @Output() google = new EventEmitter<void>();
  @Output() facebook = new EventEmitter<void>();
  @Input() label: string = 'Login';
  @Input() enabledProviders: SocialProvider[] = [];

  isEnabled(provider: SocialProvider): boolean {
    return this.enabledProviders.includes(provider);
  }
}
