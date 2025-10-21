import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-check-email',
  templateUrl: './check-email.component.html',
  styleUrls: ['./check-email.component.scss'],
  imports: [CommonModule]
})
export class CheckEmailComponent {
  constructor(private readonly authService: AuthService) {}

  startLogin(event?: Event): void {
    event?.preventDefault();
    this.authService.startLogin();
  }
}

