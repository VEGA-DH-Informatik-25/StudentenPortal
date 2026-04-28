import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { GroupsPage } from './groups-page';
import { Groups } from '../../../core/services/groups';
import { CampusGroup } from '../../../core/models/group.model';

describe('GroupsPage', () => {
  let component: GroupsPage;
  let fixture: ComponentFixture<GroupsPage>;

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
      imports: [GroupsPage],
      providers: [
        provideRouter([]),
        {
          provide: Groups,
          useValue: {
            getGroups: () => of([group]),
            createGroup: () => of({ ...group, id: 'group-2', type: 'Social', canManage: true }),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GroupsPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
