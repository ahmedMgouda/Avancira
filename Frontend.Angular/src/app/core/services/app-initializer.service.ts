import { inject, Injectable } from '@angular/core';
import { catchError, firstValueFrom, from, of } from 'rxjs';

import { ConfigService } from '../../services/config.service';
import { AuthService } from '../auth/services/auth.service';
import { LoggerService } from '../logging/services/logger.service';
import { NetworkService } from '../network/services/network.service';

@Injectable({ providedIn: 'root' })
export class AppInitializerService {
  private readonly configService = inject(ConfigService);
  private readonly authService = inject(AuthService);
  private readonly logger = inject(LoggerService);
  private readonly network = inject(NetworkService);

  async initialize(): Promise<void> {
    this.showLoader('Loading...');

    try {
      this.network.getStatus();
      await this.restoreSession();
      this.logger.info('[AppInit] Initialization completed');
    } catch (err) {
      this.logger.error('[AppInit] Initialization failed', err);
      this.updateStatus('Something went wrong. Please refresh.');
    } finally {
      await this.delay(400);
      this.hideLoader();
    }
  }

  private async restoreSession(): Promise<void> {
    await firstValueFrom(
      from(this.authService.init()).pipe(
        catchError(err => {
          this.logger.warn('[AppInit] Auth restore failed, continuing as guest', err);
          return of(null);
        })
      )
    );
  }

  /** DOM helpers */
  private showLoader(message: string) {
    this.updateStatus(message);
    const loader = document.getElementById('app-init-loader');
    if (loader) loader.style.display = 'flex';
  }

  private updateStatus(message: string) {
    const el = document.getElementById('init-status');
    if (el) el.textContent = message;
  }

  private hideLoader() {
    const loader = document.getElementById('app-init-loader');
    if (loader) {
      loader.classList.add('app-ready');
      document.body.classList.add('app-ready');
      setTimeout(() => loader.remove(), 500);
    }
  }

  private delay(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}
