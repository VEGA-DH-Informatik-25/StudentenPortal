import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { ContactProfile } from '../../../core/models/contact.model';

@Component({
  selector: 'app-favorite-contact-card',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './favorite-contact-card.html',
  styleUrl: './favorite-contact-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FavoriteContactCard {
  readonly contact = input.required<ContactProfile>();
  readonly favoriteToggle = output<ContactProfile>();

  protected readonly initials = computed(() => {
    const parts = this.contact().displayName
      .split(/[.\s_-]+/)
      .filter(Boolean);

    return parts.length
      ? parts.slice(0, 2).map(part => part[0].toUpperCase()).join('')
      : '?';
  });

  protected toggleFavorite(): void {
    this.favoriteToggle.emit(this.contact());
  }
}
