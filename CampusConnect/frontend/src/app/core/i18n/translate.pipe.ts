import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18n } from './i18n';
import { TranslationKey } from './translations';

@Pipe({
  name: 'translate',
  standalone: true,
  pure: false,
})
export class TranslatePipe implements PipeTransform {
  private readonly _i18n = inject(I18n);

  transform(key: TranslationKey, params: Record<string, string | number> = {}): string {
    return this._i18n.translate(key, params);
  }
}