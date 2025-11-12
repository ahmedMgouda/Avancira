import { DOCUMENT } from '@angular/common';
import { effect, inject, Injectable, signal } from '@angular/core';

import { StorageService } from '../../services/storage.service';

type ThemeMode = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly storage = inject(StorageService);
  private readonly document = inject(DOCUMENT);

  private readonly storageKey = 'avancira.theme';
  private readonly mediaQuery = this.document?.defaultView?.matchMedia?.('(prefers-color-scheme: dark)') ?? null;
  private readonly prefersDark = this.mediaQuery?.matches ?? false;

  private readonly themeSignal = signal<ThemeMode>(this.getInitialTheme());

  readonly theme = this.themeSignal.asReadonly();

  private hasUserPreference = this.storage.getItem(this.storageKey) !== null;

  constructor() {
    effect(() => {
      this.applyTheme(this.themeSignal());
    });

    if (this.mediaQuery) {
      const listener = (event: MediaQueryListEvent) => {
        if (!this.hasUserPreference) {
          this.themeSignal.set(event.matches ? 'dark' : 'light');
        }
      };

      if ('addEventListener' in this.mediaQuery) {
        this.mediaQuery.addEventListener('change', listener);
      } else {
        this.mediaQuery.addListener(listener);
      }
    }
  }

  setTheme(theme: ThemeMode): void {
    if (this.themeSignal() === theme) {
      return;
    }

    this.themeSignal.set(theme);
    this.storage.setItem(this.storageKey, theme);
    this.hasUserPreference = true;
  }

  toggleTheme(): void {
    this.setTheme(this.themeSignal() === 'dark' ? 'light' : 'dark');
  }

  private getInitialTheme(): ThemeMode {
    const stored = this.storage.getItem(this.storageKey);
    if (stored === 'dark' || stored === 'light') {
      return stored;
    }

    return this.prefersDark ? 'dark' : 'light';
  }

  private applyTheme(theme: ThemeMode): void {
    const documentElement = this.document.documentElement;
    documentElement.classList.toggle('dark-theme', theme === 'dark');
    documentElement.classList.toggle('light-theme', theme === 'light');
    documentElement.setAttribute('data-theme', theme);
    documentElement.style.colorScheme = theme;

    const body = this.document.body;
    if (body) {
      body.dataset.theme = theme;
    }
  }
}
