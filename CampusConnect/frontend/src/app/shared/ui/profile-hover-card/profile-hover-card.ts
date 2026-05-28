import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { I18n } from '../../../core/i18n/i18n';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { ContactProfile } from '../../../core/models/contact.model';

@Component({
  selector: 'app-profile-hover-card',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './profile-hover-card.html',
  styleUrl: './profile-hover-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileHoverCard {
  private static _nextTooltipId = 0;
  private readonly _i18n = inject(I18n);

  readonly profile = input<ContactProfile | null | undefined>(null);
  readonly displayName = input.required<string>();
  readonly showAvatar = input(false);
  readonly showName = input(true);
  protected readonly _tooltipId = `profile-hover-card-${ProfileHoverCard._nextTooltipId++}`;

  protected readonly _initials = computed(() => {
    const source = this.profile()?.displayName || this.displayName();
    const parts = source
      .replace(/@.*/, '')
      .split(/[.\s_-]+/)
      .filter(Boolean);

    return parts.length === 0
      ? 'CC'
      : parts.slice(0, 2).map(part => part[0].toUpperCase()).join('');
  });

  protected roleLabel(role: string): string {
    return this._i18n.roleLabel(role);
  }
}
