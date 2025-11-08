import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { ConfirmationDialogComponent } from "./components/confirmation-dialog/confirmation-dialog.component";
import { GlobalLoaderComponent } from "./core/loading/components/global-loader.component";
import { TopProgressBarComponent } from "./core/loading/components/top-progress-bar.component";

import { AuthService } from './core/auth/services/auth.service';

import { ToastContainerComponent } from './core/toast';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    ToastContainerComponent,
    ConfirmationDialogComponent,
    GlobalLoaderComponent,
    TopProgressBarComponent
],
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
  constructor(private readonly auth: AuthService) { }

  async ngOnInit(): Promise<void> {
    try {
      await this.auth.init();

      queueMicrotask(() => {
        console.log(
          this.auth.isAuthenticated()
            ? '[App] Session restored'
            : '[App] Anonymous user'
        );
      });
    } catch (e) {
      console.error('[App] Init failed', e);
    }
  }
}

