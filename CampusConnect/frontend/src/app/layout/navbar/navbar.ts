import { Component, ChangeDetectionStrategy, ElementRef, HostListener, ViewChild, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { I18n } from '../../core/i18n/i18n';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Auth } from '../../core/services/auth';
import { Theme, ThemePreference } from '../../core/services/theme';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Navbar {
  protected readonly _auth = inject(Auth);
  protected readonly _i18n = inject(I18n);
  protected readonly _theme = inject(Theme);
  protected readonly _isMenuOpen = signal(false);
  protected readonly _themePreferences: ThemePreference[] = ['system', 'light', 'dark'];
  @ViewChild('profileMenu') private readonly _profileMenu?: ElementRef<HTMLDetailsElement>;
  @ViewChild('settingsMenu') private readonly _settingsMenu?: ElementRef<HTMLDetailsElement>;

  protected readonly _profileInitials = computed(() => {
    const displayName = this._auth.displayName().trim();
    const fallback = this._auth.userProfile()?.email ?? '';
    const source = displayName || fallback;
    const parts = source
      .replace(/@.*/, '')
      .split(/[.\s_-]+/)
      .filter(Boolean);

    if (parts.length === 0) {
      return '?';
    }

    return parts
      .slice(0, 2)
      .map(part => part[0].toUpperCase())
      .join('');
  });

  protected setLanguage(language: string): void {
    this._i18n.setLanguage(language);
  }

  protected setThemePreference(preference: ThemePreference): void {
    this._theme.setPreference(preference);
  }

  protected themePreferenceLabel(preference: ThemePreference): string {
    switch (preference) {
      case 'dark':
        return this._i18n.translate('theme.dark');
      case 'light':
        return this._i18n.translate('theme.light');
      case 'system':
      default:
        return this._i18n.translate('theme.system');
    }
  }

  protected toggleMenu(): void {
    this._isMenuOpen.update(isOpen => !isOpen);
  }

  protected closeMenu(): void {
    this._isMenuOpen.set(false);
  }

  protected roleLabel(role: string): string {
    return this._i18n.roleLabel(role);
  }

  @HostListener('document:click', ['$event.target'])
  protected closeMenusOnOutsideClick(target: EventTarget | null): void {
    if (!(target instanceof Node)) {
      return;
    }

    for (const menu of [this._settingsMenu?.nativeElement, this._profileMenu?.nativeElement]) {
      if (menu?.open && !menu.contains(target)) {
        menu.open = false;
      }
    }
  }
}

