import { Component, OnInit } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { ToastrService } from 'ngx-toastr';

import { ConfirmationDialogComponent } from "./components/confirmation-dialog/confirmation-dialog.component";

import { AuthService } from './services/auth.service';
import { ConfigService } from './services/config.service';
import { NotificationService } from './services/notification.service';

import { NotificationEvent } from './models/enums/notification-event';

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

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) {
      this.toastr.info('Your session expired. Please sign in again.');
      this.authService.logout(false);
      return;
    }

    this.configService.loadConfig().subscribe({
      next: () => console.log('Config loaded:', this.configService.get('apiUrl')),
      error: (err) => console.error('Failed to load configuration:', err.message),
    });

    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.currentRoute = event.urlAfterRedirects;
      }
    });

    this.notificationService.startConnection(this.authService.getAccessToken() ?? "");

    this.notificationService.onReceiveNotification((notification) => {
      if (notification.eventName === NotificationEvent.NewMessage) {
        // Only show toast notification for messages if NOT on the messages page
        // The message-thread component will handle displaying messages when on the messages page
        if (this.currentRoute !== '/messages') {
          this.toastr.info(notification.data.content, notification.message);
        }
      } else {
        // For all other notification types, show the toast
        this.toastr.info(notification.data.content, notification.message);
      }
    });
  }
}
