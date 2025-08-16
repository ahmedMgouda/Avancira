import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class StorageService {
  private isBrowser(): boolean {
    return typeof window !== 'undefined' && !!window.localStorage;
  }

  getItem(key: string): string | null {
    return this.isBrowser() ? window.localStorage.getItem(key) : null;
  }

  setItem(key: string, value: string): void {
    if (this.isBrowser()) {
      window.localStorage.setItem(key, value);
    }
  }

  removeItem(key: string): void {
    if (this.isBrowser()) {
      window.localStorage.removeItem(key);
    }
  }
}
