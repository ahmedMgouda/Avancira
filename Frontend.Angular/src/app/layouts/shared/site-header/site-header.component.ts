import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

import { AuthService } from '../../../services/auth.service';
import { ImageFallbackDirective } from '../../../directives/image-fallback.directive';

@Component({
  selector: 'app-site-header',
  standalone: true,
  imports: [CommonModule, RouterModule, ImageFallbackDirective],
  templateUrl: './site-header.component.html',
  styleUrls: ['./site-header.component.scss']
})
export class SiteHeaderComponent {
  constructor(public readonly authService: AuthService) {}

  startLogin(event?: Event): void {
    event?.preventDefault();
    this.authService.startLogin();
  }

  logout(): void {
    this.authService.logout();
  }
}
