import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { FeedPost } from '../../../core/models/feed.model';
import { CampusGroup } from '../../../core/models/group.model';
import { Auth } from '../../../core/services/auth';
import { Feed } from '../../../core/services/feed';
import { Groups } from '../../../core/services/groups';
import { GroupDetailPage } from './group-detail-page';

describe('GroupDetailPage', () => {
  let fixture: ComponentFixture<GroupDetailPage>;
  let component: GroupDetailPage;
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
    name: 'Lerngruppe Web',
    description: 'Gemeinsam lernen',
    type: 'Social',
    audience: 'Interessierte',
    courseCode: null,
    ownerUserId: 'user-1',
    ownerLabel: 'Alice',
    iconLabel: 'LW',
    accentColor: '#2563eb',
    assignedUserCount: 1,
    canManage: true,
    isAssigned: true,
    canPost: true,
    canJoin: false,
    memberPermission: 'ReadWrite',
    settings: { allowStudentPosts: true, allowComments: true, requiresApproval: false, isDiscoverable: true },
  };

  const posts: FeedPost[] = [
    {
      id: 'post-1',
      authorName: 'Alice',
      group,
      content: 'Treffen um 16 Uhr',
      createdAt: '2026-01-01T10:00:00Z',
      canDelete: false,
      canComment: true,
      comments: [],
      reactions: [],
    },
    {
      id: 'post-2',
      authorName: 'Bob',
      group: { ...group, id: 'group-2', name: 'Andere Gruppe' },
      content: 'Soll nicht sichtbar sein',
      createdAt: '2026-01-01T11:00:00Z',
      canDelete: false,
      canComment: true,
      comments: [],
      reactions: [],
    },
  ];

  beforeEach(async () => {
    feedApi = {
      getFeed: vi.fn(() => of(posts)),
      createPost: vi.fn(() => of({ ...posts[0], id: 'post-new', content: 'Neue Info' })),
      deletePost: vi.fn(() => of(undefined)),
      createComment: vi.fn(() => of(posts[0])),
      deleteComment: vi.fn(() => of(posts[0])),
      toggleReaction: vi.fn(() => of({ ...posts[0], reactions: [{ emoji: '🚀', count: 1, reactedByCurrentUser: true }] })),
    };

    await TestBed.configureTestingModule({
      imports: [GroupDetailPage],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'group-1' } } } },
        { provide: Auth, useValue: { displayName: signal('Alice') } },
        { provide: Groups, useValue: { getGroups: () => of([group]), joinGroup: () => of(group) } },
        { provide: Feed, useValue: feedApi },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GroupDetailPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('shows the selected group posts', () => {
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Lerngruppe Web');
    expect(text).toContain('Treffen um 16 Uhr');
    expect(text).not.toContain('Soll nicht sichtbar sein');
  });

  it('creates posts for the selected group', () => {
    fixture.detectChanges();

    (component as any).updateContent('Neue Info');
    (component as any).onPost();

    expect(feedApi.createPost).toHaveBeenCalledWith({ content: 'Neue Info', groupId: 'group-1' });
  });
});
