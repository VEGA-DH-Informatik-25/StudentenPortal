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
      settings: { allowStudentPosts: true, allowComments: true, requiresApproval: false, isDiscoverable: true },
    },
    accounts: [
      { id: 'user-1', displayName: 'Alice', email: 'alice@dhbw-loerrach.de', role: 'Student', course: 'TIF25A', isAssigned: true },
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
});
