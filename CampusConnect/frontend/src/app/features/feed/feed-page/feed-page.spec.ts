import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WritableSignal, signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { FeedPage } from './feed-page';
import { Auth } from '../../../core/services/auth';
import { Feed } from '../../../core/services/feed';
import { Groups } from '../../../core/services/groups';
import { Timetable } from '../../../core/services/timetable';
import { CampusGroup } from '../../../core/models/group.model';
import { TimetableResponse } from '../../../core/models/timetable.model';

describe('FeedPage', () => {
  let component: FeedPage;
  let fixture: ComponentFixture<FeedPage>;
  let feedApi: {
    getFeed: ReturnType<typeof vi.fn>;
    createPost: ReturnType<typeof vi.fn>;
    deletePost: ReturnType<typeof vi.fn>;
    createComment: ReturnType<typeof vi.fn>;
    deleteComment: ReturnType<typeof vi.fn>;
    toggleReaction: ReturnType<typeof vi.fn>;
  };
  let timetableApi: {
    getStoredCourse: ReturnType<typeof vi.fn>;
    normalizeCourse: ReturnType<typeof vi.fn>;
    getTimetable: ReturnType<typeof vi.fn>;
  };
  let userProfile: WritableSignal<{ course: string; role: string } | null>;

  const group: CampusGroup = {
    id: 'group-1',
    name: 'Kurs TIF25A',
    description: 'Kursgruppe',
    type: 'Course',
    audience: 'TIF25A',
    courseCode: 'TIF25A',
    ownerUserId: null,
    ownerLabel: 'Informatik',
    iconLabel: 'TI',
    accentColor: '#e2001a',
    assignedUserCount: 0,
    canManage: false,
    isAssigned: true,
    canPost: true,
    canJoin: false,
    memberPermission: 'ReadWrite',
    settings: { allowStudentPosts: true, allowComments: true, requiresApproval: false, isDiscoverable: true },
  };

  beforeEach(async () => {
    const post = { id: 'post-1', authorName: 'Alice', group, content: 'Hallo', createdAt: new Date().toISOString(), canDelete: true, canComment: true, comments: [], reactions: [] };
    feedApi = {
      getFeed: vi.fn(() => of([])),
      createPost: vi.fn(() => of(post)),
      deletePost: vi.fn(() => of(undefined)),
      createComment: vi.fn(() => of(post)),
      deleteComment: vi.fn(() => of(post)),
      toggleReaction: vi.fn(() => of(post)),
    };
    userProfile = signal({ course: 'TIF25A', role: 'Student' });
    timetableApi = {
      getStoredCourse: vi.fn(() => 'TIF25A'),
      normalizeCourse: vi.fn((course: string) => course.trim().toUpperCase()),
      getTimetable: vi.fn(() => of(createTimetable([]))),
    };

    await TestBed.configureTestingModule({
      imports: [FeedPage],
      providers: [
        provideRouter([]),
        {
          provide: Auth,
          useValue: {
            displayName: signal('Alice'),
            userRole: signal('Student'),
            userProfile,
          },
        },
        {
          provide: Feed,
          useValue: feedApi,
        },
        {
          provide: Groups,
          useValue: { getGroups: () => of([group]) },
        },
        {
          provide: Timetable,
          useValue: timetableApi,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(FeedPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('opens the comment composer from the compact comment button', () => {
    fixture.detectChanges();
    (component as any)._posts.set([{ id: 'post-1', authorName: 'Alice', group, content: 'Hallo', createdAt: new Date().toISOString(), canDelete: true, canComment: true, comments: [], reactions: [] }]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.comment-composer')).toBeNull();
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    buttons.find(button => button.textContent?.includes('Kommentieren'))?.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.comment-composer')).not.toBeNull();
  });

  it('submits a picked emoji reaction', () => {
    fixture.detectChanges();
    const post = { id: 'post-1', authorName: 'Alice', group, content: 'Hallo', createdAt: new Date().toISOString(), canDelete: true, canComment: true, comments: [], reactions: [] };
    (component as any)._posts.set([post]);

    (component as any).onPickReaction(post, '🚀');

    expect(feedApi.toggleReaction).toHaveBeenCalledWith('post-1', { emoji: '🚀' });
  });

  it('loads and sorts the current day schedule', () => {
    const laterEvent = createEvent('later', '2026-04-29T11:00:00+02:00', '2026-04-29T12:00:00+02:00');
    const earlierEvent = createEvent('earlier', '2026-04-29T09:00:00+02:00', '2026-04-29T10:00:00+02:00');
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-04-29T08:00:00+02:00'));
    timetableApi.getTimetable.mockReturnValue(of(createTimetable([laterEvent, earlierEvent])));

    fixture.detectChanges();

    expect(component['_scheduleCourse']()).toBe('TIF25A');
    expect(component['_scheduleEvents']().map(event => event.id)).toEqual(['earlier', 'later']);
    expect(component['_scheduleError']()).toBe('');

    vi.useRealTimers();
  });

  it('shows a course selection action when no schedule course is available', () => {
    userProfile.set(null);
    timetableApi.getStoredCourse.mockReturnValue('');

    fixture.detectChanges();

    expect(timetableApi.getTimetable).not.toHaveBeenCalled();
    expect(component['_scheduleError']()).toBe('Wähle im Stundenplan zuerst deinen Kurs aus.');
  });

  it('clears schedule events and shows an error when schedule loading fails', () => {
    timetableApi.getTimetable.mockReturnValue(throwError(() => new Error('network')));

    fixture.detectChanges();

    expect(component['_scheduleEvents']()).toEqual([]);
    expect(component['_scheduleError']()).toBe('Der Tagesplan konnte nicht geladen werden.');
  });

  it('clears stale feed errors on a successful reload and prevents duplicate posts', () => {
    fixture.detectChanges();
    component['_error'].set('Alter Fehler');

    component['_loadFeed']();

    expect(component['_error']()).toBe('');

    component['updateContent']('Neuer Beitrag');
    component['_isPosting'].set(true);
    component['onPost']();

    expect(feedApi.createPost).not.toHaveBeenCalled();
  });
});

function createTimetable(events: ReturnType<typeof createEvent>[]): TimetableResponse {
  return {
    course: 'TIF25A',
    timezone: 'Europe/Berlin',
    days: [{ date: '2026-04-29', events }],
  };
}

function createEvent(id: string, start: string, end: string) {
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
