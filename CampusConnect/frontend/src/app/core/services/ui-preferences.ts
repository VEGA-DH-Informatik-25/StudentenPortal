import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class UiPreferences {
  getString(key: string): string {
    return localStorage.getItem(key) ?? '';
  }

  setString(key: string, value: string): void {
    if (!value) {
      localStorage.removeItem(key);
      return;
    }

    localStorage.setItem(key, value);
  }

  getJson<T>(key: string, fallback: T, isValid: (value: unknown) => value is T): T {
    const raw = localStorage.getItem(key);
    if (!raw) {
      return fallback;
    }

    try {
      const parsed: unknown = JSON.parse(raw);
      return isValid(parsed) ? parsed : fallback;
    } catch {
      return fallback;
    }
  }

  setJson<T>(key: string, value: T): void {
    localStorage.setItem(key, JSON.stringify(value));
  }
}
