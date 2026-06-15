import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { I18n } from '../../../core/i18n/i18n';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { ContactProfile } from '../../../core/models/contact.model';

@Component({
  selector: 'app-contact-result-card',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './contact-result-card.html',
  styleUrl: './contact-result-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactResultCard {
  private readonly _i18n = inject(I18n);

  readonly contact = input.required<ContactProfile>();
  readonly isFavorite = input(false);
  readonly favoriteToggle = output<ContactProfile>();

  protected readonly initials = computed(() => {
    const parts = this.contact().displayName
      .split(/[.\s_-]+/)
      .filter(Boolean);

    return parts.length
      ? parts.slice(0, 2).map(part => part[0].toUpperCase()).join('')
      : '?';
  });

  protected roleLabel(role: string): string {
    return this._i18n.roleLabel(role);
  }

  protected semesterLabel(semester: number | null): string {
    return semester === null
      ? this._i18n.translate('common.noSemester')
      : this._i18n.translate('common.semesterValue', { semester });
  }

  protected toggleFavorite(): void {
    this.favoriteToggle.emit(this.contact());
  }
}
