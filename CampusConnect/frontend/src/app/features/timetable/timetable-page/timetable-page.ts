import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { I18n } from '../../../core/i18n/i18n';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { TimetableEvent, TimetableDay } from '../../../core/models/timetable.model';
import { Auth } from '../../../core/services/auth';
import { Courses } from '../../../core/services/courses';
import { Timetable } from '../../../core/services/timetable';

type TimetableView = 'list' | 'week' | 'day';

interface CalendarTimeline {
  startMinutes: number;
  endMinutes: number;
  spanMinutes: number;
  height: number;
  hours: number[];
}

const DEFAULT_TIMELINE_START = 8 * 60;
const DEFAULT_TIMELINE_END = 18 * 60;
const TIMELINE_PIXELS_PER_MINUTE = 0.85;
const COMPACT_EVENT_MAX_HEIGHT = 96;
const DEFAULT_TIMETABLE_LOOKAHEAD_DAYS = 120;
const WEEK_VIEW_DAYS = 6;
const DAY_VIEW_DAYS = 1;

interface TimetableRange {
  days: number;
  from?: string;
}

@Component({
  selector: 'app-timetable-page',
  standalone: true,
  imports: [DatePipe, FormsModule, TranslatePipe],
  templateUrl: './timetable-page.html',
  styleUrl: './timetable-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TimetablePage implements OnInit {
  private readonly _auth = inject(Auth);
  private readonly _coursesService = inject(Courses);
  protected readonly _i18n = inject(I18n);
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

  protected readonly _calendarTimeline = computed<CalendarTimeline>(() => {
    const events = this._calendarDays()
      .flatMap(day => day.events)
      .filter(event => !event.isAllDay);

    const eventStarts = events.map(event => this._eventStartMinutes(event));
    const eventEnds = events.map(event => this._eventEndMinutes(event));

    const earliestStart = eventStarts.length > 0 ? Math.min(...eventStarts) : DEFAULT_TIMELINE_START;
    const latestEnd = eventEnds.length > 0 ? Math.max(...eventEnds) : DEFAULT_TIMELINE_END;
    const startMinutes = Math.max(0, Math.min(DEFAULT_TIMELINE_START, Math.floor(earliestStart / 60) * 60));
    const endMinutes = Math.min(24 * 60, Math.max(DEFAULT_TIMELINE_END, Math.ceil(latestEnd / 60) * 60));
    const spanMinutes = Math.max(60, endMinutes - startMinutes);
    const hourCount = Math.floor(spanMinutes / 60) + 1;

    return {
      startMinutes,
      endMinutes: startMinutes + spanMinutes,
      spanMinutes,
      height: Math.max(420, Math.round(spanMinutes * TIMELINE_PIXELS_PER_MINUTE)),
      hours: Array.from({ length: hourCount }, (_, index) => (startMinutes / 60) + index),
    };
  });

  ngOnInit(): void {
    this._coursesService.getCourses().subscribe({
      next: courses => this._initializeCourseOptions(courses.map(course => course.code)),
      error: () => this._initializeCourseOptions([]),
    });
  }

  private _initializeCourseOptions(courseCodes: string[]): void {
    const profileCourse = this._profileCourse();
    const courseOptions = profileCourse
      ? [...new Set([profileCourse, ...this._timetableService.getCourseOptions(courseCodes)])].sort((a, b) => a.localeCompare(b, 'de', { numeric: true, sensitivity: 'base' }))
      : this._timetableService.getCourseOptions(courseCodes);
    this._courseOptions.set(courseOptions);

    const initialCourse = profileCourse || this._timetableService.getStoredCourse();
    if (initialCourse) {
      this._setCourseSelection(initialCourse);
      this._loadCourse(initialCourse);
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
    this._loadVisibleRange();
  }

  protected previousRange(): void {
    this._moveAnchor(this._activeView() === 'week' ? -7 : -1);
    this._loadVisibleRange();
  }

  protected nextRange(): void {
    this._moveAnchor(this._activeView() === 'week' ? 7 : 1);
    this._loadVisibleRange();
  }

  protected jumpToToday(): void {
    this._anchorDate.set(this._dateKey(new Date()));
    this._loadVisibleRange();
  }

  protected refresh(): void {
    const selectedCourse = this._courseSelection() === 'custom'
      ? this._customCourse()
      : this._courseSelection();

    this._loadVisibleRange(selectedCourse);
  }

  private _profileCourse(): string {
    const profile = this._auth.userProfile();
    if (!profile || profile.role === 'Admin') {
      return '';
    }

    return this._timetableService.normalizeCourse(profile.course);
  }

  protected dayBadge(date: string): string | null {
    const today = this._dateKey(new Date());
    const tomorrow = this._dateKey(new Date(Date.now() + 24 * 60 * 60 * 1000));

    if (date === today) {
      return this._i18n.translate('common.today');
    }

    return date === tomorrow ? this._i18n.translate('common.tomorrow') : null;
  }

  protected eventTime(event: TimetableEvent): string {
    if (event.isAllDay) {
      return this._i18n.translate('common.allDay');
    }

    return `${this._formatTime(event.start)}-${this._formatTime(event.end)}`;
  }

  protected eventDuration(event: TimetableEvent): string | null {
    if (event.isAllDay) {
      return null;
    }

    const durationMinutes = this._eventDurationMinutes(event);
    if (durationMinutes < 60) {
      return `${durationMinutes} min`;
    }

    const hours = Math.floor(durationMinutes / 60);
    const minutes = durationMinutes % 60;
    const hourLabel = this._i18n.translate('common.hourShort');

    return minutes === 0 ? `${hours} ${hourLabel}` : `${hours} ${hourLabel} ${minutes} min`;
  }

  protected timelineHourOffset(hour: number): number {
    return this._percentage((hour * 60) - this._calendarTimeline().startMinutes);
  }

  protected timelineHourLabel(hour: number): string {
    return `${String(hour).padStart(2, '0')}:00`;
  }

  protected calendarEventOffset(event: TimetableEvent): number {
    if (event.isAllDay) {
      return 0;
    }

    const timeline = this._calendarTimeline();
    const offset = Math.max(0, this._eventStartMinutes(event) - timeline.startMinutes);
    return this._percentage(offset);
  }

  protected calendarEventHeight(event: TimetableEvent): number {
    if (event.isAllDay) {
      return 100;
    }

    const timeline = this._calendarTimeline();
    const start = Math.max(this._eventStartMinutes(event), timeline.startMinutes);
    const end = Math.min(this._eventEndMinutes(event), timeline.endMinutes);
    return this._percentage(Math.max(1, end - start));
  }

  protected calendarEventIsCompact(event: TimetableEvent): boolean {
    if (event.isAllDay) {
      return false;
    }

    return this._renderedEventHeight(event) < COMPACT_EVENT_MAX_HEIGHT;
  }

  protected calendarEventLabel(event: TimetableEvent): string {
    const duration = this.eventDuration(event);
    const time = duration ? `${this.eventTime(event)} (${duration})` : this.eventTime(event);
    const details = [
      time,
      event.title,
      this.compactMeta(event),
      event.isOnline ? 'Online' : null,
    ].filter((value): value is string => Boolean(value));

    return details.join(' | ');
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
      this._error.set(this._i18n.translate('timetable.courseMissing'));
      return;
    }

    this._isLoading.set(true);
    this._error.set(null);

    this._anchorDate.set(this._dateKey(new Date()));
    this._fetchTimetable(normalizedCourse, this._visibleRange(), true);
  }

  private _loadVisibleRange(course = this._selectedCourse()): void {
    const normalizedCourse = this._timetableService.normalizeCourse(course);
    if (!normalizedCourse || !this._course()) {
      return;
    }

    const range = this._visibleRange();
    this._fetchTimetable(normalizedCourse, range, this._activeView() === 'list');
  }

  private _fetchTimetable(normalizedCourse: string, range: TimetableRange, replaceDays: boolean): void {
    this._isLoading.set(true);
    this._error.set(null);

    this._timetableService.getTimetable(normalizedCourse, range.days, range.from ?? '').subscribe({
      next: timetable => {
        this._course.set(timetable.course);
        this._timezone.set(timetable.timezone);
        this._days.set(replaceDays ? timetable.days : this._mergeDays(this._days(), timetable.days, range));
        this._timetableService.storeCourse(timetable.course);
        this._courseOptions.set(this._timetableService.getCourseOptions(this._courseOptions()));
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

  private _selectedCourse(): string {
    return this._courseSelection() === 'custom'
      ? this._customCourse()
      : this._courseSelection();
  }

  private _visibleRange(): TimetableRange {
    if (this._activeView() === 'week') {
      return { from: this._dateKey(this._weekStart(this._fromDateKey(this._anchorDate()))), days: WEEK_VIEW_DAYS };
    }

    if (this._activeView() === 'day') {
      return { from: this._anchorDate(), days: DAY_VIEW_DAYS };
    }

    return { days: DEFAULT_TIMETABLE_LOOKAHEAD_DAYS };
  }

  private _mergeDays(currentDays: TimetableDay[], incomingDays: TimetableDay[], range: TimetableRange): TimetableDay[] {
    const merged = new Map(currentDays.map(day => [day.date, day]));

    if (range.from) {
      const start = this._fromDateKey(range.from);
      for (let index = 0; index < range.days; index++) {
        const date = new Date(start);
        date.setDate(start.getDate() + index);
        merged.delete(this._dateKey(date));
      }
    } else {
      merged.clear();
    }

    for (const day of incomingDays) {
      merged.set(day.date, day);
    }

    return [...merged.values()].sort((first, second) => first.date.localeCompare(second.date));
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
      return body?.error ?? this._i18n.translate('timetable.loadError');
    }

    return this._i18n.translate('timetable.loadError');
  }

  private _formatTime(value: string): string {
    return new Intl.DateTimeFormat(this._i18n.locale(), {
      hour: '2-digit',
      minute: '2-digit',
      timeZone: this._timezone(),
    }).format(new Date(value));
  }

  private _eventStartMinutes(event: TimetableEvent): number {
    return this._minutesInTimezone(event.start);
  }

  private _eventEndMinutes(event: TimetableEvent): number {
    const startMinutes = this._eventStartMinutes(event);
    const endMinutes = this._minutesInTimezone(event.end);
    return endMinutes <= startMinutes ? endMinutes + (24 * 60) : endMinutes;
  }

  private _eventDurationMinutes(event: TimetableEvent): number {
    const duration = Math.round((new Date(event.end).getTime() - new Date(event.start).getTime()) / 60000);
    return Math.max(1, duration);
  }

  private _minutesInTimezone(value: string): number {
    const parts = new Intl.DateTimeFormat('en-GB', {
      hour: '2-digit',
      hourCycle: 'h23',
      minute: '2-digit',
      timeZone: this._timezone(),
    }).formatToParts(new Date(value));
    const hour = Number(parts.find(part => part.type === 'hour')?.value ?? 0) % 24;
    const minute = Number(parts.find(part => part.type === 'minute')?.value ?? 0);

    return (hour * 60) + minute;
  }

  private _percentage(minutes: number): number {
    return (minutes / this._calendarTimeline().spanMinutes) * 100;
  }

  private _renderedEventHeight(event: TimetableEvent): number {
    return (this.calendarEventHeight(event) / 100) * this._calendarTimeline().height;
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
    return new Intl.DateTimeFormat(this._i18n.locale(), { day: '2-digit', month: '2-digit' }).format(this._fromDateKey(value));
  }

  private _formatDateLong(value: string): string {
    return new Intl.DateTimeFormat(this._i18n.locale(), { weekday: 'long', day: '2-digit', month: 'long' }).format(this._fromDateKey(value));
  }
}
