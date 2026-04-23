import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Mensa } from '../../../core/services/mensa';
import { MensaDay } from '../../../core/models/mensa.model';

@Component({
  selector: 'app-mensa-page',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './mensa-page.html',
  styleUrl: './mensa-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MensaPage implements OnInit {
  private readonly _mensaService = inject(Mensa);

  protected readonly _menu = signal<MensaDay[]>([]);
  protected readonly _isLoading = signal(false);
  protected readonly _selectedDay = signal(0);

  ngOnInit(): void {
    this._isLoading.set(true);
    this._mensaService.getWeekMenu().subscribe({
      next: menu => { this._menu.set(menu); this._isLoading.set(false); },
      error: () => this._isLoading.set(false),
    });
  }

  protected selectDay(index: number): void {
    this._selectedDay.set(index);
  }

  protected get currentDay(): MensaDay | null {
    return this._menu()[this._selectedDay()] ?? null;
  }
}

