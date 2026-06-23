import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';

export type ThemePreference = 'system' | 'light' | 'dark';
export type EffectiveTheme = 'light' | 'dark';

const STORAGE_KEY = 'campusconnect.theme';
const DARK_MEDIA_QUERY = '(prefers-color-scheme: dark)';

@Injectable({ providedIn: 'root' })
export class Theme {
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _systemTheme = signal<EffectiveTheme>(this._readSystemTheme());
  private readonly _preference = signal<ThemePreference>(this._initialPreference());
  private readonly _mediaQuery = globalThis.matchMedia?.(DARK_MEDIA_QUERY) ?? null;

  readonly preference = this._preference.asReadonly();
  readonly effectiveTheme = computed<EffectiveTheme>(() => {
    const preference = this._preference();
    return preference === 'system' ? this._systemTheme() : preference;
  });

  constructor() {
    this._listenToSystemTheme();
    this._applyTheme(this.effectiveTheme());
  }

  setPreference(preference: string): void {
    if (!this._isThemePreference(preference)) {
      return;
    }

    this._preference.set(preference);
    globalThis.localStorage?.setItem(STORAGE_KEY, preference);
    this._applyTheme(this.effectiveTheme());
  }

  private _initialPreference(): ThemePreference {
    const storedPreference = globalThis.localStorage?.getItem(STORAGE_KEY);
    if (storedPreference && this._isThemePreference(storedPreference)) {
      return storedPreference;
    }

    if (storedPreference) {
      globalThis.localStorage?.removeItem(STORAGE_KEY);
    }

    return 'system';
  }

  private _isThemePreference(preference: string): preference is ThemePreference {
    return preference === 'system' || preference === 'light' || preference === 'dark';
  }

  private _readSystemTheme(): EffectiveTheme {
    return globalThis.matchMedia?.(DARK_MEDIA_QUERY).matches ? 'dark' : 'light';
  }

  private _listenToSystemTheme(): void {
    if (!this._mediaQuery) {
      return;
    }

    const listener = (event: MediaQueryListEvent): void => {
      this._systemTheme.set(event.matches ? 'dark' : 'light');
      if (this._preference() === 'system') {
        this._applyTheme(this._systemTheme());
      }
    };

    if (this._mediaQuery.addEventListener) {
      this._mediaQuery.addEventListener('change', listener);
      this._destroyRef.onDestroy(() => this._mediaQuery?.removeEventListener?.('change', listener));
      return;
    }

    this._mediaQuery.addListener?.(listener);
    this._destroyRef.onDestroy(() => this._mediaQuery?.removeListener?.(listener));
  }

  private _applyTheme(theme: EffectiveTheme): void {
    const root = globalThis.document?.documentElement;
    if (!root) {
      return;
    }

    root.dataset['theme'] = theme;
    root.style.colorScheme = theme;
  }
}
