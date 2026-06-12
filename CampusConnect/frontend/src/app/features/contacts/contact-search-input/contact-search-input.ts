import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  selector: 'app-contact-search-input',
  standalone: true,
  imports: [FormsModule, TranslatePipe],
  templateUrl: './contact-search-input.html',
  styleUrl: './contact-search-input.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactSearchInput {
  readonly query = input('');
  readonly queryChange = output<string>();
}
