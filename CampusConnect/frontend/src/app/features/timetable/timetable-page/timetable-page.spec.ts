import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WritableSignal, signal } from '@angular/core';
import { of } from 'rxjs';

import { TimetableDay, TimetableEvent } from '../../../core/models/timetable.model';
import { Auth } from '../../../core/services/auth';
import { Courses } from '../../../core/services/courses';
import { Timetable } from '../../../core/services/timetable';
import { TimetablePage } from './timetable-page';

interface TimetablePageHarness {
  _activeView: WritableSignal<'list' | 'week' | 'day'>;
  _anchorDate: WritableSignal<string>;
  _course: WritableSignal<string>;
  _courseSelection: WritableSignal<string>;
  _days: WritableSignal<TimetableDay[]>;
  _calendarDays: () => TimetableDay[];
  _calendarTimeline: () => { startMinutes: number; endMinutes: number; spanMinutes: number };
  calendarEventHeight(event: TimetableEvent): number;
  calendarEventOffset(event: TimetableEvent): number;
  calendarEventIsCompact(event: TimetableEvent): boolean;
  calendarEventLabel(event: TimetableEvent): string;
  eventDuration(event: TimetableEvent): string | null;
  previousRange(): void;
}

interface TimetableRequest {
  course: string;
  days: number;
  from: string;
}

describe('TimetablePage', () => {
  let component: TimetablePageHarness;
  let fixture: ComponentFixture<TimetablePage>;
  let timetableRequests: TimetableRequest[];
  let timetableResponses: Map<string, TimetableDay[]>;

  beforeEach(async () => {
    timetableRequests = [];
    timetableResponses = new Map<string, TimetableDay[]>();

    await TestBed.configureTestingModule({
      imports: [TimetablePage],
      providers: [
        provideHttpClient(),
        {
          provide: Auth,
          useValue: {
            userProfile: signal(null),
          },
        },
        {
          provide: Courses,
          useValue: {
            getCourses: () => of([]),
          },
        },
        {
          provide: Timetable,
          useValue: {
            getTimetable: (course = '', days = 120, from = '') => {
              const normalizedCourse = course.trim().toUpperCase();
              timetableRequests.push({ course: normalizedCourse, days, from });
              return of({ course: normalizedCourse, timezone: 'Europe/Berlin', days: timetableResponses.get(from) ?? [] });
            },
            getCourseOptions: (courseCodes: string[] = []) => courseCodes,
            getStoredCourse: () => '',
            storeCourse: () => undefined,
            normalizeCourse: (course: string) => course.trim().toUpperCase(),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TimetablePage);
    component = fixture.componentInstance as unknown as TimetablePageHarness;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should size week events proportionally to their duration', () => {
    const lecture = createEvent('lecture-1', '2026-04-27T09:00:00+02:00', '2026-04-27T10:30:00+02:00');

    component._activeView.set('week');
    component._anchorDate.set('2026-04-27');
    component._days.set([{ date: '2026-04-27', events: [lecture] }]);

    expect(component._calendarTimeline().spanMinutes).toBe(600);
    expect(component.calendarEventOffset(lecture)).toBeCloseTo(10);
    expect(component.calendarEventHeight(lecture)).toBeCloseTo(15);
    expect(component.eventDuration(lecture)).toBe('1 Std. 30 min');
  });

  it('should keep past days visible in the selected week', () => {
    const lecture = createEvent('lecture-6', '2026-04-27T09:00:00+02:00', '2026-04-27T10:30:00+02:00');

    component._activeView.set('week');
    component._anchorDate.set('2026-04-29');
    component._days.set([{ date: '2026-04-27', events: [lecture] }]);

    const calendarDays = component._calendarDays();

    expect(calendarDays[0].date).toBe('2026-04-27');
    expect(calendarDays[0].events).toEqual([lecture]);
  });

  it('should load the visible past week when navigating backwards', () => {
    const lecture = createEvent('lecture-9', '2026-05-05T09:00:00+02:00', '2026-05-05T10:30:00+02:00');
    timetableResponses.set('2026-05-04', [{ date: '2026-05-05', events: [lecture] }]);

    component._course.set('TIF25A');
    component._courseSelection.set('TIF25A');
    component._activeView.set('week');
    component._anchorDate.set('2026-05-11');

    component.previousRange();

    expect(timetableRequests.at(-1)).toEqual({ course: 'TIF25A', days: 6, from: '2026-05-04' });
    expect(component._calendarDays()[1].date).toBe('2026-05-05');
    expect(component._calendarDays()[1].events).toEqual([lecture]);
  });

  it('should keep day view scoped to the selected date', () => {
    const firstLecture = createEvent('lecture-7', '2026-04-27T09:00:00+02:00', '2026-04-27T10:30:00+02:00');
    const selectedLecture = createEvent('lecture-8', '2026-04-28T09:00:00+02:00', '2026-04-28T10:30:00+02:00');

    component._activeView.set('day');
    component._anchorDate.set('2026-04-28');
    component._days.set([
      { date: '2026-04-27', events: [firstLecture] },
      { date: '2026-04-28', events: [selectedLecture] },
    ]);

    expect(component._calendarDays()).toEqual([{ date: '2026-04-28', events: [selectedLecture] }]);
  });

  it('should mark short events as compact without losing their details', () => {
    const shortLecture = createEvent('lecture-4', '2026-04-27T08:30:00+02:00', '2026-04-27T09:00:00+02:00');
    const regularLecture = createEvent('lecture-5', '2026-04-27T09:00:00+02:00', '2026-04-27T12:15:00+02:00');

    component._activeView.set('week');
    component._anchorDate.set('2026-04-27');
    component._days.set([{ date: '2026-04-27', events: [shortLecture, regularLecture] }]);

    expect(component.calendarEventIsCompact(shortLecture)).toBe(true);
    expect(component.calendarEventIsCompact(regularLecture)).toBe(false);
    expect(component.calendarEventLabel(shortLecture)).toContain('08:30-09:00 (30 min)');
    expect(component.calendarEventLabel(shortLecture)).toContain('Software Engineering');
    expect(component.calendarEventLabel(shortLecture)).toContain('Aula');
  });

  it('should extend the timeline when events are outside the regular day', () => {
    const earlyLecture = createEvent('lecture-2', '2026-04-27T07:30:00+02:00', '2026-04-27T08:30:00+02:00');
    const lateLecture = createEvent('lecture-3', '2026-04-27T18:15:00+02:00', '2026-04-27T19:15:00+02:00');

    component._activeView.set('week');
    component._anchorDate.set('2026-04-27');
    component._days.set([{ date: '2026-04-27', events: [earlyLecture, lateLecture] }]);

    expect(component._calendarTimeline().startMinutes).toBe(7 * 60);
    expect(component._calendarTimeline().endMinutes).toBe(20 * 60);
  });
});

function createEvent(id: string, start: string, end: string): TimetableEvent {
  return {
    id,
    title: 'Software Engineering',
    start,
    end,
    location: 'Aula',
    description: null,
    isAllDay: false,
    isOnline: false,
  };
}
