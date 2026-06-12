import { ChangeDetectionStrategy, Component, HostListener, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, debounceTime, distinctUntilChanged, map, of, Subject, switchMap } from 'rxjs';
import { I18n } from '../../../core/i18n/i18n';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { ContactProfile } from '../../../core/models/contact.model';
import { Contacts } from '../../../core/services/contacts';
import { ContactSearchInput } from '../contact-search-input/contact-search-input';
import { ContactSearchResults } from '../contact-search-results/contact-search-results';

@Component({
  selector: 'app-contact-search-modal',
  standalone: true,
  imports: [ContactSearchInput, ContactSearchResults, TranslatePipe],
  templateUrl: './contact-search-modal.html',
  styleUrl: './contact-search-modal.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactSearchModal {
  private readonly _contactsService = inject(Contacts);
  private readonly _i18n = inject(I18n);
  private readonly _queryChanges = new Subject<string>();

  readonly isOpen = input(false);
  readonly favoriteIds = input<ReadonlySet<string>>(new Set<string>());
  readonly closed = output<void>();
  readonly favoriteToggle = output<ContactProfile>();

  protected readonly _contacts = signal<ContactProfile[]>([]);
  protected readonly _error = signal('');
  protected readonly _isLoading = signal(false);
  protected readonly _query = signal('');
  protected readonly _resultLimit = 10;

  constructor() {
    this._queryChanges.pipe(
      map(query => query.trim()),
      debounceTime(350),
      distinctUntilChanged(),
      switchMap(query => {
        if (query.length < 3) {
          this._contacts.set([]);
          this._error.set('');
          this._isLoading.set(false);
          return of([]);
        }

        this._error.set('');
        this._isLoading.set(true);
        return this._contactsService.searchContacts(query, this._resultLimit).pipe(
          map(contacts => contacts.slice(0, this._resultLimit)),
          catchError(() => {
            this._error.set(this._i18n.translate('contacts.loadError'));
            return of([]);
          })
        );
      }),
      takeUntilDestroyed()
    ).subscribe(contacts => {
      this._contacts.set(contacts);
      this._isLoading.set(false);
    });
  }

  protected updateQuery(query: string): void {
    this._query.set(query);
    this._queryChanges.next(query);
  }

  protected close(): void {
    this._query.set('');
    this._queryChanges.next('');
    this._contacts.set([]);
    this._error.set('');
    this._isLoading.set(false);
    this.closed.emit();
  }

  protected stopClick(event: MouseEvent): void {
    event.stopPropagation();
  }

  @HostListener('document:keydown.escape')
  protected closeOnEscape(): void {
    if (this.isOpen()) {
      this.close();
    }
  }
}
