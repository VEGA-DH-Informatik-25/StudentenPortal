import { Component, ChangeDetectionStrategy, ElementRef, HostListener, ViewChild, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { I18n } from '../../core/i18n/i18n';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Auth } from '../../core/services/auth';

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
  protected readonly _isMenuOpen = signal(false);
  @ViewChild('profileMenu') private readonly _profileMenu?: ElementRef<HTMLDetailsElement>;

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

  protected toggleMenu(): void {
    this._isMenuOpen.update(isOpen => !isOpen);
  }

  protected closeMenu(): void {
    this._isMenuOpen.set(false);
  }

  protected roleLabel(role: string): string {
    return this._i18n.roleLabel(role);
  }

  protected semesterLabel(semester: number | null): string {
    return semester === null
      ? this._i18n.translate('common.noSemester')
      : this._i18n.translate('common.semesterValue', { semester });
  }

  @HostListener('document:click', ['$event.target'])
  protected closeProfileMenuOnOutsideClick(target: EventTarget | null): void {
    const profileMenu = this._profileMenu?.nativeElement;

    if (!profileMenu?.open || !(target instanceof Node) || profileMenu.contains(target)) {
      return;
    }

    profileMenu.open = false;
  }
}

