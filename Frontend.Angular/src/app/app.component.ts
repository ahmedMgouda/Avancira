import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { ConfirmationDialogComponent } from "./components/confirmation-dialog/confirmation-dialog.component";

import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ConfirmationDialogComponent],
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
  constructor(private readonly auth: AuthService) {}

  async ngOnInit(): Promise<void> {
    try {
      await this.auth.init();
      console.log(this.auth.isAuthenticated()
        ? '[App] Session restored'
        : '[App] Anonymous user');
    } catch (e) {
      console.error('[App] Init failed', e);
    }
  }
}

