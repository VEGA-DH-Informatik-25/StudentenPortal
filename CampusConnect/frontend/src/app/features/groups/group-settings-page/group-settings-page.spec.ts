import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { GroupSettingsPage } from './group-settings-page';
import { Groups } from '../../../core/services/groups';
import { GroupSettingsDetails } from '../../../core/models/group.model';

describe('GroupSettingsPage', () => {
  let component: GroupSettingsPage;
  let fixture: ComponentFixture<GroupSettingsPage>;

  const details: GroupSettingsDetails = {
    group: {
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
      groupRole: 'Owner',
      isSystemAdminAccess: false,
      canAppointModerator: true,
      settings: { allowStudentPosts: true, allowComments: true, requiresApproval: false, isDiscoverable: true },
    },
    accounts: [
      { id: 'user-1', displayName: 'Alice', email: 'alice@dhbw-loerrach.de', role: 'Student', course: 'TIF25A', isAssigned: true, permission: 'ReadWrite', groupRole: 'Member' },
      { id: 'user-2', displayName: 'Bob', email: 'bob@dhbw-loerrach.de', role: 'Lecturer', course: 'TIF25B', isAssigned: false, permission: 'ReadWrite', groupRole: 'None' },
    ],
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
            updateAssignments: () => of(details),
            updateMemberPermissions: () => of(details),
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

  it('filters assignment rows by search text', () => {
    (component as any)._accountSearch.set('bob');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Bob');
    expect(text).not.toContain('alice@dhbw-loerrach.de');
  });

  it('derives group roles from ownership and permissions', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    const owner = details.accounts[0];
    const other = details.accounts[1];

    expect((component as any).accountGroupRole(owner)).toBe('Owner');
    expect((component as any).accountGroupRole(other)).toBe('None');

    (component as any)._selectedAccountIds.set(['user-2']);
    (component as any)._selectedPermissions.set({ 'user-2': 'Manage' });
    expect((component as any).accountGroupRole(other)).toBe('Moderator');

    (component as any)._selectedPermissions.set({ 'user-2': 'ReadWrite' });
    expect((component as any).accountGroupRole(other)).toBe('Member');
  });
});
