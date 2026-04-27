import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TimetableEvent, TimetableDay } from '../../../core/models/timetable.model';
import { Timetable } from '../../../core/services/timetable';

type TimetableView = 'list' | 'week' | 'day';

@Component({
  selector: 'app-timetable-page',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './timetable-page.html',
  styleUrl: './timetable-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TimetablePage implements OnInit {
  private readonly _timetableService = inject(Timetable);

  protected readonly _courseOptions = signal<string[]>([]);
  protected readonly _courseSelection = signal('');
  protected readonly _customCourse = signal('');
  protected readonly _days = signal<TimetableDay[]>([]);
  protected readonly _course = signal('');
  protected readonly _timezone = signal('Europe/Berlin');
  protected readonly _isLoading = signal(false);
  protected readonly _error = signal<string | null>(null);
  protected readonly _activeView = signal<TimetableView>('list');
  protected readonly _anchorDate = signal(this._dateKey(new Date()));

  protected readonly _eventCount = computed(() =>
    this._days().reduce((count, day) => count + day.events.length, 0)
  );

  protected readonly _calendarDays = computed(() => {
    const byDate = new Map(this._days().map(day => [day.date, day]));
    const anchor = this._fromDateKey(this._anchorDate());
    const start = this._activeView() === 'week' ? this._weekStart(anchor) : anchor;
    const count = this._activeView() === 'week' ? 6 : 1;

    return Array.from({ length: count }, (_, index) => {
      const date = new Date(start);
      date.setDate(start.getDate() + index);
      const key = this._dateKey(date);
      return byDate.get(key) ?? { date: key, events: [] };
    });
  });

  protected readonly _rangeTitle = computed(() => {
    const days = this._calendarDays();
    if (this._activeView() === 'day') {
      return this._formatDateLong(days[0]?.date ?? this._anchorDate());
    }

    const first = days[0]?.date ?? this._anchorDate();
    const last = days.at(-1)?.date ?? first;
    return `${this._formatDateShort(first)} - ${this._formatDateShort(last)}`;
  });

  ngOnInit(): void {
    this._courseOptions.set(this._timetableService.getCourseOptions());
    const storedCourse = this._timetableService.getStoredCourse();
    if (storedCourse) {
      this._setCourseSelection(storedCourse);
      this._loadCourse(storedCourse);
    }
  }

  protected onCourseSelectionChange(course: string): void {
    this._courseSelection.set(course);
    this._error.set(null);

    if (course && course !== 'custom') {
      this._customCourse.set('');
      this._loadCourse(course);
    }
  }

  protected loadCustomCourse(): void {
    this._loadCourse(this._customCourse());
  }

  protected changeCourse(): void {
    this._course.set('');
    this._days.set([]);
    this._error.set(null);
  }

  protected selectCourse(course: string): void {
    this._setCourseSelection(course);
    this._loadCourse(course);
  }

  protected selectView(view: TimetableView): void {
    this._activeView.set(view);
  }

  protected previousRange(): void {
    this._moveAnchor(this._activeView() === 'week' ? -7 : -1);
  }

  protected nextRange(): void {
    this._moveAnchor(this._activeView() === 'week' ? 7 : 1);
  }

  protected jumpToToday(): void {
    this._anchorDate.set(this._dateKey(new Date()));
  }

  protected refresh(): void {
    const selectedCourse = this._courseSelection() === 'custom'
      ? this._customCourse()
      : this._courseSelection();

    this._loadCourse(selectedCourse);
  }

  protected dayBadge(date: string): string | null {
    const today = this._dateKey(new Date());
    const tomorrow = this._dateKey(new Date(Date.now() + 24 * 60 * 60 * 1000));

    if (date === today) {
      return 'Heute';
    }

    return date === tomorrow ? 'Morgen' : null;
  }

  protected eventTime(event: TimetableEvent): string {
    if (event.isAllDay) {
      return 'Ganzer Tag';
    }

    return `${this._formatTime(event.start)}-${this._formatTime(event.end)}`;
  }

  protected visibleDescription(event: TimetableEvent): string | null {
    if (!event.description) {
      return null;
    }

    const trimmed = event.description.replace(/\s+/g, ' ').trim();
    if (!trimmed || trimmed === event.location) {
      return null;
    }

    return trimmed.length > 140 ? `${trimmed.slice(0, 140)}...` : trimmed;
  }

  protected compactMeta(event: TimetableEvent): string | null {
    return event.location ?? this.visibleDescription(event);
  }

  private _loadCourse(course: string): void {
    const normalizedCourse = this._timetableService.normalizeCourse(course);
    if (!normalizedCourse) {
      this._error.set('Bitte einen Kurs auswählen.');
      return;
    }

    this._isLoading.set(true);
    this._error.set(null);

    this._timetableService.getTimetable(normalizedCourse).subscribe({
      next: timetable => {
        this._course.set(timetable.course);
        this._timezone.set(timetable.timezone);
        this._days.set(timetable.days);
        this._anchorDate.set(this._dateKey(new Date()));
        this._timetableService.storeCourse(timetable.course);
        this._courseOptions.set(this._timetableService.getCourseOptions());
        this._setCourseSelection(timetable.course);
        this._isLoading.set(false);
      },
      error: error => {
        this._error.set(this._readError(error));
        this._days.set([]);
        this._course.set(normalizedCourse);
        this._isLoading.set(false);
      },
    });
  }

  private _setCourseSelection(course: string): void {
    const normalizedCourse = this._timetableService.normalizeCourse(course);
    if (this._courseOptions().includes(normalizedCourse)) {
      this._courseSelection.set(normalizedCourse);
      this._customCourse.set('');
    } else {
      this._courseSelection.set('custom');
      this._customCourse.set(normalizedCourse);
    }
  }

  private _readError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as { error?: string } | null;
      return body?.error ?? 'Der Vorlesungsplan konnte nicht geladen werden.';
    }

    return 'Der Vorlesungsplan konnte nicht geladen werden.';
  }

  private _formatTime(value: string): string {
    return new Intl.DateTimeFormat('de-DE', {
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(value));
  }

  private _dateKey(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private _moveAnchor(days: number): void {
    const date = this._fromDateKey(this._anchorDate());
    date.setDate(date.getDate() + days);
    this._anchorDate.set(this._dateKey(date));
  }

  private _fromDateKey(date: string): Date {
    const [year, month, day] = date.split('-').map(Number);
    return new Date(year, month - 1, day);
  }

  private _weekStart(date: Date): Date {
    const weekStart = new Date(date);
    const distanceFromMonday = (weekStart.getDay() + 6) % 7;
    weekStart.setDate(weekStart.getDate() - distanceFromMonday);
    return weekStart;
  }

  private _formatDateShort(value: string): string {
    return new Intl.DateTimeFormat('de-DE', { day: '2-digit', month: '2-digit' }).format(this._fromDateKey(value));
  }

  private _formatDateLong(value: string): string {
    return new Intl.DateTimeFormat('de-DE', { weekday: 'long', day: '2-digit', month: 'long' }).format(this._fromDateKey(value));
  }
}
