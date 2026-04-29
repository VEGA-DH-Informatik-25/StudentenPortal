import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { FeedPage } from './feed-page';
import { Auth } from '../../../core/services/auth';
import { Feed } from '../../../core/services/feed';
import { Groups } from '../../../core/services/groups';
import { Timetable } from '../../../core/services/timetable';
import { CampusGroup } from '../../../core/models/group.model';

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

    await TestBed.configureTestingModule({
      imports: [FeedPage],
      providers: [
        provideRouter([]),
        {
          provide: Auth,
          useValue: {
            displayName: signal('Alice'),
            userRole: signal('Student'),
            userProfile: signal({ course: 'TIF25A', role: 'Student' }),
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
          useValue: {
            getStoredCourse: () => 'TIF25A',
            normalizeCourse: (course: string) => course.trim().toUpperCase(),
            getTimetable: () => of({ course: 'TIF25A', timezone: 'Europe/Berlin', days: [] }),
          },
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
});
