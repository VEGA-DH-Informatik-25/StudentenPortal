import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { GroupSettingsPage } from './group-settings-page';
import { Groups } from '../../../core/services/groups';
import { Courses } from '../../../core/services/courses';
import { GroupSettingsDetails } from '../../../core/models/group.model';

describe('GroupSettingsPage', () => {
  let component: GroupSettingsPage;
  let fixture: ComponentFixture<GroupSettingsPage>;

  const details: GroupSettingsDetails = {
    group: {
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
      assignedUserCount: 2,
      isAssigned: true,
      canManage: true,
      canEditSettings: true,
      canManageMembers: true,
      canAppointModerator: true,
      canPost: true,
      canInteract: true,
      canJoin: false,
      canRequestJoin: false,
      hasPendingJoinRequest: false,
      hasPendingInvitation: false,
      pendingJoinRequestCount: 0,
      groupRole: 'Owner',
      isSystemAdminAccess: false,
      isCourseManaged: false,
      settings: { allowStudentPosts: true, allowComments: true, requiresApproval: false, isDiscoverable: true, joinRule: 'Open' },
    },
    members: [
      { id: 'user-1', displayName: 'Alice', email: 'alice@dhbw-loerrach.de', role: 'Student', course: 'TIF25A', groupRole: 'Owner', isOwner: true },
      { id: 'user-2', displayName: 'Bob', email: 'bob@dhbw-loerrach.de', role: 'Lecturer', course: 'TIF25B', groupRole: 'Member', isOwner: false },
    ],
    joinRequests: [],
    invitations: [],
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GroupSettingsPage],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'group-1' } } },
        },
        {
          provide: Groups,
          useValue: {
            getSettings: () => of(details),
            updateSettings: () => of(details.group),
            searchCandidates: () => of([]),
            addMembers: () => of(details),
            addCourse: () => of(details),
            removeMember: () => of(details),
            setMemberRole: () => of(details),
          },
        },
        {
          provide: Courses,
          useValue: {
            getCourses: () => of([]),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GroupSettingsPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('lists current members with their group role', () => {
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Alice');
    expect(text).toContain('Bob');
  });

  it('allows role editing for non-owner members only', () => {
    fixture.detectChanges();

    const owner = details.members[0];
    const member = details.members[1];

    expect((component as any).canEditMemberRole(owner)).toBe(false);
    expect((component as any).canEditMemberRole(member)).toBe(true);
  });
});
