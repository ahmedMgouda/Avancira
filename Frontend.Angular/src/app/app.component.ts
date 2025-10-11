import { Component, OnInit } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { ToastrService } from 'ngx-toastr';

import { ConfirmationDialogComponent } from "./components/confirmation-dialog/confirmation-dialog.component";

import { AuthService } from './services/auth.service';
import { ConfigService } from './services/config.service';
import { NotificationService } from './services/notification.service';

import { ConfigKey } from './models/config-key';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ConfirmationDialogComponent],
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
  currentRoute: string = '';
  constructor(
    private notificationService: NotificationService,
    private authService: AuthService,
    private toastr: ToastrService,
    private router: Router,
    private configService: ConfigService
  ) { }

  async ngOnInit(): Promise<void> {
    this.configService.loadConfig().subscribe({
      next: () => console.log('Config loaded:', this.configService.get(ConfigKey.StripePublishableKey)),
      error: (err) => console.error('Failed to load configuration:', err.message),
    });

    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.currentRoute = event.urlAfterRedirects;
      }
    });


    // Initialize auth state on app startup
    // - Restores tokens from sessionStorage
    // - Loads user profile and permissions if valid tokens exist
    await this.authService.init();
  }
}
