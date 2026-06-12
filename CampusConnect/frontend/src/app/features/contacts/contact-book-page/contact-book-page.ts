import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { ContactProfile } from '../../../core/models/contact.model';
import { ContactSearchCard } from '../contact-search-card/contact-search-card';
import { ContactSearchModal } from '../contact-search-modal/contact-search-modal';
import { FavoritesDropdown } from '../favorites-dropdown/favorites-dropdown';

@Component({
  selector: 'app-contact-book-page',
  standalone: true,
  imports: [ContactSearchCard, ContactSearchModal, FavoritesDropdown, TranslatePipe],
  templateUrl: './contact-book-page.html',
  styleUrl: './contact-book-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactBookPage {
  protected readonly _favoriteContacts = signal<ContactProfile[]>([]);
  protected readonly _favoriteIds = computed(() => new Set(this._favoriteContacts().map(contact => contact.id)));
  protected readonly _isSearchOpen = signal(false);

  protected openSearch(): void {
    this._isSearchOpen.set(true);
  }

  protected closeSearch(): void {
    this._isSearchOpen.set(false);
  }

  protected toggleFavorite(contact: ContactProfile): void {
    this._favoriteContacts.update(favorites => {
      const isFavorite = favorites.some(favorite => favorite.id === contact.id);
      return isFavorite
        ? favorites.filter(favorite => favorite.id !== contact.id)
        : [...favorites, contact].sort((a, b) => a.displayName.localeCompare(b.displayName, 'de', { sensitivity: 'base' }));
    });
  }
}
