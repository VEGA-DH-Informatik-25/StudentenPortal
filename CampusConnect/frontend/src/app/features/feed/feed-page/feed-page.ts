import { Component, ChangeDetectionStrategy, computed, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Auth } from '../../../core/services/auth';
import { Feed } from '../../../core/services/feed';
import { FeedPost } from '../../../core/models/feed.model';
import { TimetableEvent } from '../../../core/models/timetable.model';
import { Timetable } from '../../../core/services/timetable';

@Component({
  selector: 'app-feed-page',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './feed-page.html',
  styleUrl: './feed-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeedPage implements OnInit {
  private readonly _auth = inject(Auth);
  private readonly _feedService = inject(Feed);
  private readonly _router = inject(Router);
  private readonly _timetableService = inject(Timetable);

  protected readonly _posts = signal<FeedPost[]>([]);
  protected readonly _isLoading = signal(false);
  protected readonly _error = signal('');
  protected readonly _newContent = signal('');
  protected readonly _displayName = computed(() => this._auth.displayName() || 'Studierende');
  protected readonly _profileInitials = computed(() => this._initialsFor(this._displayName()));
  protected readonly _scheduleEvents = signal<TimetableEvent[]>([]);
  protected readonly _scheduleCourse = signal('');
  protected readonly _scheduleDate = signal(this._dateKey(new Date()));
  protected readonly _scheduleTimezone = signal('Europe/Berlin');
  protected readonly _scheduleIsLoading = signal(false);
  protected readonly _scheduleError = signal('');
  protected readonly _scheduleTitle = computed(() => this._formatDateLong(this._scheduleDate()));

  ngOnInit(): void {
    this._loadFeed();
    this._loadTodaySchedule();
  }

  private _loadFeed(): void {
    this._isLoading.set(true);
    this._feedService.getFeed().subscribe({
      next: posts => { this._posts.set(posts); this._isLoading.set(false); },
      error: () => { this._error.set('Feed konnte nicht geladen werden.'); this._isLoading.set(false); },
    });
  }

  protected onPost(): void {
    const content = this._newContent().trim();
    if (!content) return;
    this._error.set('');
    this._feedService.createPost({ content }).subscribe({
      next: post => { this._posts.update(posts => [post, ...posts]); this._newContent.set(''); },
      error: () => this._error.set('Beitrag konnte nicht erstellt werden.'),
    });
  }

  protected onDelete(id: string): void {
    this._feedService.deletePost(id).subscribe({
      next: () => this._posts.update(posts => posts.filter(post => post.id !== id)),
    });
  }

  protected updateContent(value: string): void {
    this._newContent.set(value);
  }

  protected navigateTo(route: string): void {
    void this._router.navigate([route]);
  }

  protected scheduleTime(event: TimetableEvent): string {
    if (event.isAllDay) {
      return 'Ganzer Tag';
    }

    return `${this._formatTime(event.start)}-${this._formatTime(event.end)}`;
  }

  protected scheduleDuration(event: TimetableEvent): string | null {
    if (event.isAllDay) {
      return null;
    }

    const durationMinutes = Math.max(1, Math.round((new Date(event.end).getTime() - new Date(event.start).getTime()) / 60000));
    if (durationMinutes < 60) {
      return `${durationMinutes} min`;
    }

    const hours = Math.floor(durationMinutes / 60);
    const minutes = durationMinutes % 60;
    return minutes === 0 ? `${hours} Std.` : `${hours} Std. ${minutes} min`;
  }

  protected scheduleMeta(event: TimetableEvent): string | null {
    if (event.location) {
      return event.location;
    }

    if (!event.description) {
      return null;
    }

    const trimmed = event.description.replace(/\s+/g, ' ').trim();
    if (!trimmed) {
      return null;
    }

    return trimmed.length > 90 ? `${trimmed.slice(0, 90)}...` : trimmed;
  }

  protected initialsFor(value: string): string {
    return this._initialsFor(value);
  }

  private _loadTodaySchedule(): void {
    const course = this._resolveScheduleCourse();
    if (!course) {
      this._scheduleError.set('Wähle im Stundenplan zuerst deinen Kurs aus.');
      return;
    }

    const today = this._dateKey(new Date());
    this._scheduleDate.set(today);
    this._scheduleCourse.set(course);
    this._scheduleIsLoading.set(true);
    this._scheduleError.set('');

    this._timetableService.getTimetable(course, 14).subscribe({
      next: timetable => {
        const todaySchedule = timetable.days.find(day => day.date === today);
        this._scheduleCourse.set(timetable.course);
        this._scheduleTimezone.set(timetable.timezone);
        this._scheduleEvents.set([...(todaySchedule?.events ?? [])].sort((first, second) =>
          new Date(first.start).getTime() - new Date(second.start).getTime()
        ));
        this._scheduleIsLoading.set(false);
      },
      error: () => {
        this._scheduleEvents.set([]);
        this._scheduleError.set('Der Tagesplan konnte nicht geladen werden.');
        this._scheduleIsLoading.set(false);
      },
    });
  }

  private _resolveScheduleCourse(): string {
    const storedCourse = this._timetableService.getStoredCourse();
    if (storedCourse) {
      return storedCourse;
    }

    const profile = this._auth.userProfile();
    if (!profile || profile.role === 'Admin') {
      return '';
    }

    return this._timetableService.normalizeCourse(profile.course);
  }

  private _initialsFor(value: string): string {
    const parts = value
      .replace(/@.*/, '')
      .split(/[.\s_-]+/)
      .filter(Boolean);

    if (parts.length === 0) {
      return 'CC';
    }

    return parts
      .slice(0, 2)
      .map(part => part[0].toUpperCase())
      .join('');
  }

  private _formatTime(value: string): string {
    return new Intl.DateTimeFormat('de-DE', {
      hour: '2-digit',
      minute: '2-digit',
      timeZone: this._scheduleTimezone(),
    }).format(new Date(value));
  }

  private _dateKey(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private _formatDateLong(value: string): string {
    const [year, month, day] = value.split('-').map(Number);
    return new Intl.DateTimeFormat('de-DE', { weekday: 'long', day: '2-digit', month: 'long' })
      .format(new Date(year, month - 1, day));
  }
}

