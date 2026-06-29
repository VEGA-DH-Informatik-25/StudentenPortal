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
    type: 'Campus',
    audience: 'Interessierte',
    courseCode: null,
    officialCategory: null,
    ownerUserId: 'user-1',
    ownerLabel: 'Alice',
    iconLabel: 'LW',
    accentColor: '#2563eb',
    assignedUserCount: 1,
    isAssigned: true,
    canManage: true,
    canEditSettings: true,
    canManageMembers: true,
    canAppointModerator: false,
    canPost: true,
    canInteract: true,
    canJoin: false,
    canRequestJoin: false,
    hasPendingJoinRequest: false,
    hasPendingInvitation: false,
    pendingJoinRequestCount: 0,
    canDelete: true,
    groupRole: 'Member',
    isSystemAdminAccess: false,
    isCourseManaged: false,
    settings: { allowStudentPosts: true, allowComments: true, requiresApproval: false, isDiscoverable: true, joinRule: 'Open' },
  };

  const posts: FeedPost[] = [
    {
      id: 'post-1',
      authorName: 'Alice',
      group,
      content: 'Treffen um 16 Uhr',
      createdAt: '2026-01-01T10:00:00Z',
      status: 'Published',
      allowComments: true,
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
      status: 'Published',
      allowComments: true,
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
        { provide: Groups, useValue: { getGroups: () => of([group]), joinGroup: () => of(group), leaveGroup: () => of({ group: null, deleted: false }) } },
        { provide: Feed, useValue: feedApi },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GroupDetailPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('shows the selected group posts', () => {
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Lerngruppe Web');
    expect(text).toContain('Treffen um 16 Uhr');
    expect(text).not.toContain('Soll nicht sichtbar sein');
  });

  it('creates posts for the selected group from the form submit', () => {
    fixture.detectChanges();

    (component as any).updateContent('Neue Info');
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form.composer') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));

    expect(feedApi.createPost).toHaveBeenCalledWith({ content: 'Neue Info', groupId: 'group-1', allowComments: true });
  });

  it('creates comments from the comment form submit', () => {
    fixture.detectChanges();

    (component as any).toggleCommentComposer(posts[0]);
    (component as any).updateCommentDraft('post-1', 'Bin dabei');
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form.comment-composer') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));

    expect(feedApi.createComment).toHaveBeenCalledWith('post-1', { content: 'Bin dabei' });
  });

  it('submits a picked emoji reaction', () => {
    fixture.detectChanges();

    (component as any).onPickReaction(posts[0], '🚀');

    expect(feedApi.toggleReaction).toHaveBeenCalledWith('post-1', { emoji: '🚀' });
  });

  it('does not delete group posts when confirmation is cancelled', () => {
    vi.spyOn(globalThis, 'confirm').mockReturnValue(false);

    (component as any).onDelete('post-1');

    expect(feedApi.deletePost).not.toHaveBeenCalled();
  });

  it('deletes group posts after confirmation', () => {
    vi.spyOn(globalThis, 'confirm').mockReturnValue(true);

    (component as any).onDelete('post-1');

    expect(globalThis.confirm).toHaveBeenCalledWith('Diesen Beitrag endgültig löschen?');
    expect(feedApi.deletePost).toHaveBeenCalledWith('post-1');
  });
});
