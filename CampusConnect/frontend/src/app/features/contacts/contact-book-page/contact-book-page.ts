import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { ContactProfile } from '../../../core/models/contact.model';
import { UiPreferences } from '../../../core/services/ui-preferences';
import { ContactSearchCard } from '../contact-search-card/contact-search-card';
import { ContactSearchModal } from '../contact-search-modal/contact-search-modal';
import { FavoritesDropdown } from '../favorites-dropdown/favorites-dropdown';

const CONTACT_FAVORITES_KEY = 'campusconnect.contacts.favorites';

@Component({
  selector: 'app-contact-book-page',
  standalone: true,
  imports: [ContactSearchCard, ContactSearchModal, FavoritesDropdown, TranslatePipe],
  templateUrl: './contact-book-page.html',
  styleUrl: './contact-book-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactBookPage {
  private readonly _uiPreferences = inject(UiPreferences);

  protected readonly _favoriteContacts = signal<ContactProfile[]>(this._storedFavorites());
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
      const nextFavorites = isFavorite
        ? favorites.filter(favorite => favorite.id !== contact.id)
        : [...favorites, contact].sort((a, b) => a.displayName.localeCompare(b.displayName, 'de', { sensitivity: 'base' }));
      this._uiPreferences.setJson(CONTACT_FAVORITES_KEY, nextFavorites);
      return nextFavorites;
    });
  }

  private _storedFavorites(): ContactProfile[] {
    return this._uiPreferences.getJson<ContactProfile[]>(
      CONTACT_FAVORITES_KEY,
      [],
      (value): value is ContactProfile[] => Array.isArray(value) && value.every(this._isContactProfile)
    );
  }

  private _isContactProfile(value: unknown): value is ContactProfile {
    return !!value &&
      typeof value === 'object' &&
      typeof (value as ContactProfile).id === 'string' &&
      typeof (value as ContactProfile).displayName === 'string' &&
      typeof (value as ContactProfile).email === 'string' &&
      typeof (value as ContactProfile).studyProgram === 'string' &&
      typeof (value as ContactProfile).course === 'string' &&
      typeof (value as ContactProfile).phoneNumber === 'string' &&
      typeof (value as ContactProfile).location === 'string' &&
      typeof (value as ContactProfile).profileNote === 'string' &&
      typeof (value as ContactProfile).role === 'string';
  }
}
