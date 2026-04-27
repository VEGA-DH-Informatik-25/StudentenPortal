import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Mensa } from '../../../core/services/mensa';
import { MensaDay } from '../../../core/models/mensa.model';

@Component({
  selector: 'app-mensa-page',
  standalone: true,
  imports: [DatePipe, DecimalPipe],
  templateUrl: './mensa-page.html',
  styleUrl: './mensa-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MensaPage implements OnInit {
  private readonly _mensaService = inject(Mensa);

  protected readonly _menu = signal<MensaDay[]>([]);
  protected readonly _isLoading = signal(false);
  protected readonly _error = signal<string | null>(null);
  protected readonly _selectedDay = signal(0);

  ngOnInit(): void {
    this._isLoading.set(true);
    this._mensaService.getWeekMenu().subscribe({
      next: menu => {
        this._menu.set(menu);
        this._error.set(null);
        this._isLoading.set(false);
      },
      error: error => {
        this._menu.set([]);
        this._error.set(this._readError(error));
        this._isLoading.set(false);
      },
    });
  }

  protected selectDay(index: number): void {
    this._selectedDay.set(index);
  }

  protected get currentDay(): MensaDay | null {
    return this._menu()[this._selectedDay()] ?? null;
  }

  private _readError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as { error?: string } | null;
      return body?.error ?? 'Der Speiseplan konnte nicht geladen werden.';
    }

    return 'Der Speiseplan konnte nicht geladen werden.';
  }
}

