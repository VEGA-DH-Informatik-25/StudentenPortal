import { Component, ChangeDetectionStrategy, computed, inject, signal, OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { I18n } from '../../../core/i18n/i18n';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Mensa } from '../../../core/services/mensa';
import { MensaDay, MensaDish } from '../../../core/models/mensa.model';
import { UiPreferences } from '../../../core/services/ui-preferences';

const MENSA_SELECTED_DATE_KEY = 'campusconnect.mensa.selectedDate';

@Component({
  selector: 'app-mensa-page',
  standalone: true,
  imports: [DatePipe, DecimalPipe, TranslatePipe],
  templateUrl: './mensa-page.html',
  styleUrl: './mensa-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MensaPage implements OnInit {
  private readonly _mensaService = inject(Mensa);
  private readonly _uiPreferences = inject(UiPreferences);
  protected readonly _i18n = inject(I18n);

  protected readonly _menu = signal<MensaDay[]>([]);
  protected readonly _isLoading = signal(false);
  protected readonly _error = signal<string | null>(null);
  protected readonly _selectedDay = signal(0);
  protected readonly _currentDay = computed(() => this._menu()[this._selectedDay()] ?? null);

  ngOnInit(): void {
    this._isLoading.set(true);
    this._mensaService.getWeekMenu().subscribe({
      next: menu => {
        this._menu.set(menu);
        this._restoreSelectedDay(menu);
        this._error.set(null);
        this._isLoading.set(false);
      },
      error: error => {
        this._menu.set([]);
        this._error.set(this._i18n.readError(error, 'mensa.loadError'));
        this._isLoading.set(false);
      },
    });
  }

  protected selectDay(index: number): void {
    if (index < 0 || index >= this._menu().length) {
      return;
    }

    this._selectedDay.set(index);
    this._uiPreferences.setString(MENSA_SELECTED_DATE_KEY, this._menu()[index]?.date ?? '');
  }

  private _restoreSelectedDay(menu: MensaDay[]): void {
    const storedDate = this._uiPreferences.getString(MENSA_SELECTED_DATE_KEY);
    const storedIndex = storedDate ? menu.findIndex(day => day.date === storedDate) : -1;

    if (storedIndex >= 0) {
      this._selectedDay.set(storedIndex);
      return;
    }

    if (this._selectedDay() >= menu.length) {
      this._selectedDay.set(0);
    }
  }

  protected categoryMarker(category: string): string {
    const normalizedCategory = category.trim();
    if (!normalizedCategory) {
      return 'ME';
    }

    return normalizedCategory
      .split(/\s+/)
      .slice(0, 2)
      .map(part => part[0]?.toUpperCase() ?? '')
      .join('') || 'ME';
  }

  protected dishNameLines(dish: MensaDish): string[] {
    return dish.nameLines?.length ? dish.nameLines : [dish.name];
  }
}

