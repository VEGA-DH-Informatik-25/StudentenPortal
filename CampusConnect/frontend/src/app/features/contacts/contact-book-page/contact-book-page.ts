import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ContactProfile } from '../../../core/models/contact.model';
import { Contacts } from '../../../core/services/contacts';
import { ProfileHoverCard } from '../../../shared/ui/profile-hover-card/profile-hover-card';

@Component({
  selector: 'app-contact-book-page',
  standalone: true,
  imports: [FormsModule, ProfileHoverCard],
  templateUrl: './contact-book-page.html',
  styleUrl: './contact-book-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactBookPage implements OnInit {
  private readonly _contactsService = inject(Contacts);

  protected readonly _contacts = signal<ContactProfile[]>([]);
  protected readonly _query = signal('');
  protected readonly _isLoading = signal(false);
  protected readonly _error = signal('');
  protected readonly _resultLabel = computed(() => {
    const count = this._contacts().length;
    return count === 1 ? '1 Kontakt' : `${count} Kontakte`;
  });

  ngOnInit(): void {
    this.search();
  }

  protected updateQuery(value: string): void {
    this._query.set(value);
  }

  protected search(): void {
    this._isLoading.set(true);
    this._error.set('');
    this._contactsService.searchContacts(this._query()).subscribe({
      next: contacts => {
        this._contacts.set(contacts);
        this._isLoading.set(false);
      },
      error: () => {
        this._contacts.set([]);
        this._error.set('Kontakte konnten nicht geladen werden.');
        this._isLoading.set(false);
      },
    });
  }

  protected clearSearch(): void {
    this._query.set('');
    this.search();
  }

  protected roleLabel(role: string): string {
    switch (role) {
      case 'Admin':
        return 'Administration';
      case 'Lecturer':
        return 'Lehrperson';
      default:
        return 'Student';
    }
  }
}
