import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  selector: 'app-empty-favorites-state',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './empty-favorites-state.html',
  styleUrl: './empty-favorites-state.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyFavoritesState {}
