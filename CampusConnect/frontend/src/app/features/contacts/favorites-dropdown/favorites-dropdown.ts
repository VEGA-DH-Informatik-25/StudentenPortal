import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { ContactProfile } from '../../../core/models/contact.model';
import { EmptyFavoritesState } from '../empty-favorites-state/empty-favorites-state';
import { FavoriteContactCard } from '../favorite-contact-card/favorite-contact-card';

@Component({
  selector: 'app-favorites-dropdown',
  standalone: true,
  imports: [EmptyFavoritesState, FavoriteContactCard, TranslatePipe],
  templateUrl: './favorites-dropdown.html',
  styleUrl: './favorites-dropdown.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FavoritesDropdown {
  readonly favorites = input<ContactProfile[]>([]);
  readonly favoriteToggle = output<ContactProfile>();
}
