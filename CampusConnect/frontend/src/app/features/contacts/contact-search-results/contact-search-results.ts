import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { ContactProfile } from '../../../core/models/contact.model';
import { ContactResultCard } from '../contact-result-card/contact-result-card';

@Component({
  selector: 'app-contact-search-results',
  standalone: true,
  imports: [ContactResultCard, TranslatePipe],
  templateUrl: './contact-search-results.html',
  styleUrl: './contact-search-results.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactSearchResults {
  readonly contacts = input<ContactProfile[]>([]);
  readonly error = input('');
  readonly favoriteIds = input<ReadonlySet<string>>(new Set<string>());
  readonly isLoading = input(false);
  readonly query = input('');
  readonly resultLimit = input(10);
  readonly favoriteToggle = output<ContactProfile>();
}
