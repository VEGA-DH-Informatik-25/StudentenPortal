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
    settings: { allowStudentPosts: true, allowComments: true, requiresApproval: false, isDiscoverable: true },
  };

  beforeEach(async () => {
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
          useValue: {
            getFeed: () => of([]),
            createPost: () => of({ id: 'post-1', authorName: 'Alice', group, content: 'Hallo', createdAt: new Date().toISOString(), canDelete: true, canComment: true, comments: [], reactions: [] }),
            deletePost: () => of(undefined),
            createComment: () => of({ id: 'post-1', authorName: 'Alice', group, content: 'Hallo', createdAt: new Date().toISOString(), canDelete: true, canComment: true, comments: [], reactions: [] }),
            deleteComment: () => of({ id: 'post-1', authorName: 'Alice', group, content: 'Hallo', createdAt: new Date().toISOString(), canDelete: true, canComment: true, comments: [], reactions: [] }),
            toggleReaction: () => of({ id: 'post-1', authorName: 'Alice', group, content: 'Hallo', createdAt: new Date().toISOString(), canDelete: true, canComment: true, comments: [], reactions: [] }),
          },
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
});
