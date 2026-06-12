import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  selector: 'app-contact-search-card',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './contact-search-card.html',
  styleUrl: './contact-search-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactSearchCard {
  readonly searchOpen = output<void>();
}
