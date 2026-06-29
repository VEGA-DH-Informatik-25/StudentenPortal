import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { WritableSignal, signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
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
    name: 'Course TIF25A',
    description: 'Course group',
    type: 'Course',
    audience: 'TIF25A',
    courseCode: 'TIF25A',
    officialCategory: null,
    ownerUserId: null,
    ownerLabel: 'Computer Science',
    iconLabel: 'TI',
    accentColor: '#e2001a',
    assignedUserCount: 0,
    isAssigned: true,
    canManage: false,
    canEditSettings: false,
    canManageMembers: false,
    canAppointModerator: false,
    canPost: true,
    canInteract: true,
    canJoin: false,
    canRequestJoin: false,
    hasPendingJoinRequest: false,
    hasPendingInvitation: false,
    pendingJoinRequestCount: 0,
    canDelete: false,
    groupRole: 'Member',
    isSystemAdminAccess: false,
    isCourseManaged: true,
    settings: { allowStudentPosts: true, allowComments: true, requiresApproval: false, isDiscoverable: true, joinRule: 'Open' },
  };

  beforeEach(async () => {
    localStorage.clear();
    const post = { id: 'post-1', authorName: 'Alice', group, content: 'Hello', translations: null, attachments: [], createdAt: new Date().toISOString(), status: 'Published' as const, allowComments: true, canDelete: true, canComment: true, comments: [], reactions: [] };
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

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('persists the selected posting group', () => {
    (component as any).updateSelectedGroup('group-1');

    expect(localStorage.getItem('campusconnect.feed.selectedGroupId')).toBe('group-1');
  });

  it('starts with a compact composer and expands from the prompt', () => {
    fixture.detectChanges();

    expect(component['_isComposerOpen']()).toBe(false);
    expect(fixture.nativeElement.querySelector('#feed-composer-panel')).toBeNull();

    const prompt = fixture.nativeElement.querySelector('.composer__prompt') as HTMLButtonElement;
    expect(prompt.getAttribute('aria-expanded')).toBe('false');
    prompt.click();
    fixture.detectChanges();

    expect(component['_isComposerOpen']()).toBe(true);
    expect(fixture.nativeElement.querySelector('#feed-composer-panel')).not.toBeNull();
  });

  it('updates composer switch states from the settings panel', () => {
    fixture.detectChanges();

    (component as any).openComposer();
    (component as any).updateContent('Hallo Kurs');
    (component as any).toggleComposerSettings();
    fixture.detectChanges();

    const commentSwitch = fixture.debugElement.query(By.css('input[name="feed-allow-comments"]'));
    commentSwitch.triggerEventHandler('ngModelChange', false);
    fixture.detectChanges();

    expect(component['_allowComments']()).toBe(false);

    const translationSwitch = fixture.debugElement.query(By.css('input[name="feed-use-translations"]'));
    translationSwitch.triggerEventHandler('ngModelChange', true);
    fixture.detectChanges();

    expect(component['_useTranslations']()).toBe(true);
    expect(component['_translationDraft']().de).toBe('Hallo Kurs');
    expect(fixture.nativeElement.querySelector('#feed-content-de')).not.toBeNull();
  });

  it('shows, removes, and validates selected attachments', () => {
    fixture.detectChanges();
    const file = new File(['hello'], 'notice.pdf', { type: 'application/pdf' });

    (component as any).openComposer();
    (component as any).toggleComposerSettings();
    (component as any).onFilesSelected(fileList([file]));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('notice.pdf');

    (component as any).removeSelectedFile(0);
    fixture.detectChanges();

    expect(component['_selectedFiles']()).toEqual([]);
    expect(fixture.nativeElement.textContent).toContain('Keine Dateien ausgewählt.');

    (component as any).onFilesSelected(fileList([new File(['bad'], 'malware.exe')]));
    fixture.detectChanges();

    expect(component['_attachmentError']()).toBe('Dieser Dateityp wird nicht unterstützt.');
    expect(fixture.nativeElement.textContent).toContain('Dieser Dateityp wird nicht unterstützt.');

    const files = Array.from({ length: 6 }, (_, index) => new File(['hello'], `notice-${index}.pdf`, { type: 'application/pdf' }));
    (component as any).onFilesSelected(fileList(files));

    expect(component['_attachmentError']()).toBe('Du kannst höchstens 5 Dateien anhängen.');
  });

  it('renders DHBW quick access links as external redirects', () => {
    fixture.detectChanges();

    const links = Array.from(fixture.nativeElement.querySelectorAll('.quick-access__item')) as HTMLAnchorElement[];

    expect(links.map(link => link.querySelector('strong')?.textContent?.trim())).toEqual([
      'Moodle',
      'Webmail',
      'DUALIS',
      'Bibliothek',
    ]);
    expect(links.map(link => link.querySelector('small')?.textContent?.trim())).toEqual([
      'Kurse und Unterlagen',
      'E-Mails und Kalender',
      'Noten und Prüfungen',
      'Katalog und Recherche',
    ]);
    expect(links.map(link => link.href)).toEqual([
      'https://moodle.loerrach.dhbw.de/',
      'https://webmail.dhbw-loerrach.de/owa',
      'https://dualis.dhbw.de/',
      'https://dhbw-loerrach.de/bibliothek/aktuelle-informationen',
    ]);
    expect(links.every(link => link.target === '_blank' && link.rel === 'noopener noreferrer')).toBe(true);
  });

  it('opens image attachments in a preview with a download action', () => {
    fixture.detectChanges();
    const imageAttachment = {
      id: 'attachment-1',
      fileName: 'campus.png',
      contentType: 'image/png',
      sizeBytes: 2048,
      isImage: true,
      downloadUrl: '/api/feed/post-1/attachments/attachment-1',
    };
    (component as any)._posts.set([{
      id: 'post-1',
      authorName: 'Alice',
      group,
      content: 'Hello',
      translations: null,
      attachments: [imageAttachment],
      createdAt: new Date().toISOString(),
      status: 'Published',
      allowComments: true,
      canDelete: true,
      canComment: true,
      comments: [],
      reactions: [],
    }]);
    fixture.detectChanges();

    const previewButton = fixture.nativeElement.querySelector('.post-card__image-preview') as HTMLButtonElement;
    expect(previewButton.getAttribute('aria-label')).toBe('Bildvorschau für campus.png öffnen');

    previewButton.click();
    fixture.detectChanges();

    const viewer = fixture.nativeElement.querySelector('.attachment-viewer') as HTMLElement;
    expect(viewer).not.toBeNull();
    expect(viewer.textContent).toContain('campus.png');
    const download = viewer.querySelector('.attachment-viewer__download') as HTMLAnchorElement;
    expect(download.getAttribute('href')).toBe('/api/feed/post-1/attachments/attachment-1');
    expect(download.getAttribute('download')).toBe('campus.png');

    component['closeAttachmentPreviewFromEscape']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.attachment-viewer')).toBeNull();
  });

  it('opens the comment composer from the compact comment button', () => {
    fixture.detectChanges();
    (component as any)._posts.set([{ id: 'post-1', authorName: 'Alice', group, content: 'Hello', translations: null, attachments: [], createdAt: new Date().toISOString(), status: 'Published', allowComments: true, canDelete: true, canComment: true, comments: [], reactions: [] }]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.comment-composer')).toBeNull();
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    buttons.find(button => button.textContent?.includes('Kommentieren'))?.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.comment-composer')).not.toBeNull();
  });

  it('submits a picked emoji reaction', () => {
    fixture.detectChanges();
    const post = { id: 'post-1', authorName: 'Alice', group, content: 'Hello', translations: null, attachments: [], createdAt: new Date().toISOString(), status: 'Published' as const, allowComments: true, canDelete: true, canComment: true, comments: [], reactions: [] };
    (component as any)._posts.set([post]);

    (component as any).onPickReaction(post, '🚀');

    expect(feedApi.toggleReaction).toHaveBeenCalledWith('post-1', { emoji: '🚀' });
  });

  it('does not delete posts when confirmation is cancelled', () => {
    vi.spyOn(globalThis, 'confirm').mockReturnValue(false);

    (component as any).onDelete('post-1');

    expect(feedApi.deletePost).not.toHaveBeenCalled();
  });

  it('deletes posts after confirmation', () => {
    vi.spyOn(globalThis, 'confirm').mockReturnValue(true);

    (component as any).onDelete('post-1');

    expect(globalThis.confirm).toHaveBeenCalledWith('Diesen Beitrag endgültig löschen?');
    expect(feedApi.deletePost).toHaveBeenCalledWith('post-1');
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
    expect(component['_scheduleError']()).toBe('Wähle einen Kurs aus, um die nächsten Termine zu sehen.');
  });

  it('clears schedule events and shows an error when schedule loading fails', () => {
    timetableApi.getTimetable.mockReturnValue(throwError(() => new Error('network')));

    fixture.detectChanges();

    expect(component['_scheduleEvents']()).toEqual([]);
    expect(component['_scheduleError']()).toBe('Der Tagesplan konnte nicht geladen werden.');
  });

  it('clears stale feed errors on a successful reload and prevents duplicate posts', () => {
    fixture.detectChanges();
    component['_error'].set('Old error');

    component['_loadFeed']();

    expect(component['_error']()).toBe('');

    component['updateContent']('New post');
    component['_isPosting'].set(true);
    component['onPost']();

    expect(feedApi.createPost).not.toHaveBeenCalled();
  });

  it('submits translated posts with selected files', () => {
    fixture.detectChanges();
    const file = new File(['hello'], 'notice.pdf', { type: 'application/pdf' });

    component['openComposer']();
    component['updateUseTranslations'](true);
    component['updateTranslation']('de', 'Hallo');
    component['updateTranslation']('en', 'Hello');
    component['updateTranslation']('fr', 'Bonjour');
    component['onFilesSelected']({ 0: file, length: 1, item: (index: number) => index === 0 ? file : null, [Symbol.iterator]: function* () { yield file; } } as unknown as FileList);
    component['onPost']();

    expect(feedApi.createPost).toHaveBeenCalledWith({
      content: 'Hallo',
      groupId: 'group-1',
      allowComments: true,
      translations: { de: 'Hallo', en: 'Hello', fr: 'Bonjour' },
      attachments: [file],
    });
    expect(component['_isComposerOpen']()).toBe(false);
    expect(component['_isComposerSettingsOpen']()).toBe(false);
    expect(component['_useTranslations']()).toBe(false);
    expect(component['_selectedFiles']()).toEqual([]);
    expect(component['_allowComments']()).toBe(true);
  });

  it('keeps the composer open and shows translated backend validation errors', () => {
    fixture.detectChanges();
    feedApi.createPost.mockReturnValueOnce(throwError(() => new HttpErrorResponse({
      status: 400,
      error: { error: 'Fill in all translation fields.' },
    })));

    component['openComposer']();
    component['updateContent']('Hallo');
    component['onPost']();

    expect(component['_isComposerOpen']()).toBe(true);
    expect(component['_error']()).toBe('Bitte fülle alle Übersetzungsfelder aus.');
  });

  it('uses the active language for translated post content', () => {
    const post = {
      id: 'post-1',
      authorName: 'Alice',
      group,
      content: 'Deutsch',
      translations: { de: 'Deutsch', en: 'English', fr: 'Français' },
      attachments: [],
      createdAt: new Date().toISOString(),
      status: 'Published' as const,
      allowComments: true,
      canDelete: true,
      canComment: true,
      comments: [],
      reactions: [],
    };

    component['_i18n'].setLanguage('en');

    expect(component['localizedPostContent'](post)).toBe('English');
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
    location: 'Auditorium',
    description: null,
    isAllDay: false,
    isOnline: false,
  };
}

function fileList(files: File[]): FileList {
  return Object.assign({}, files, {
    length: files.length,
    item: (index: number) => files[index] ?? null,
    [Symbol.iterator]: function* () {
      yield* files;
    },
  }) as unknown as FileList;
}
