// src/app/services/google-auth.service.ts
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class GoogleAuthService {
  private initialized = false;
  private clientId = '';
  private resolveFn?: (token: string) => void;
  private rejectFn?: (reason?: unknown) => void;
  private signInInProgress = false;

  /** Initializes the Google Identity SDK (FedCM-safe) */
  async init(clientId: string): Promise<void> {
    if (this.initialized) return;
    if (!clientId) return Promise.reject('Google Client ID is required.');

    this.clientId = clientId;
    await this.loadGIS();

    const google = (window as any).google;
    if (!google?.accounts?.id) throw new Error('Google Identity not available.');

    google.accounts.id.initialize({
      client_id: this.clientId,
      callback: (response: any) => {
        if (response?.credential) {
          this.resolveFn?.(response.credential);
        } else {
          this.rejectFn?.('No credential returned.');
        }
        this.clearHandlers();
      },
      use_fedcm_for_prompt: true,
      use_fedcm_for_button: true, // optional but recommended
      ux_mode: 'popup',
    });

    this.initialized = true;
  }

  /** Triggers One Tap / FedCM and resolves ID token via the initialize() callback */
  async signIn(timeoutMs = 15000): Promise<string> {
    if (!this.initialized) return Promise.reject('GoogleAuthService not initialized.');
    if (this.signInInProgress) return Promise.reject('Google Sign-In already in progress.');
    this.signInInProgress = true;

    const google = (window as any).google;

    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        this.clearHandlers();
        reject('Google Sign-In timed out.');
      }, timeoutMs);

      this.resolveFn = (token: string) => { clearTimeout(timeout); resolve(token); };
      this.rejectFn  = (reason?: unknown) => { clearTimeout(timeout); reject(reason); };

      try {
        // IMPORTANT: no prompt callback → no display-moment APIs invoked.
        google.accounts.id.prompt();
      } catch (e) {
        this.rejectFn?.(e);
        this.clearHandlers();
      }
    });
  }

  /** Optionally render an official Google button anywhere in your UI */
  renderButton(host: HTMLElement, options?: any): void {
    const google = (window as any).google;
    if (!google?.accounts?.id) return;
    google.accounts.id.renderButton(host, {
      type: 'standard',
      theme: 'outline',
      size: 'large',
      shape: 'rectangular',
      text: 'signin_with',
      logo_alignment: 'left',
      ...options,
    });
  }

  private loadGIS(): Promise<void> {
    return new Promise((resolve, reject) => {
      const w = window as any;
      if (w.google?.accounts?.id) return resolve();

      const script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.onload = () => resolve();
      script.onerror = () => reject('Google Identity SDK not loaded.');
      document.head.appendChild(script);
    });
  }

  private clearHandlers(): void {
    this.resolveFn = undefined;
    this.rejectFn = undefined;
    this.signInInProgress = false;
  }
}
