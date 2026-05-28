import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { GroupsPage } from './groups-page';
import { Groups } from '../../../core/services/groups';
import { CampusGroup } from '../../../core/models/group.model';

describe('GroupsPage', () => {
  let component: GroupsPage;
  let fixture: ComponentFixture<GroupsPage>;
  let groupsService: {
    getGroups: ReturnType<typeof vi.fn>;
    createGroup: ReturnType<typeof vi.fn>;
    joinGroup: ReturnType<typeof vi.fn>;
  };

  const group: CampusGroup = {
    id: 'group-1',
    name: 'Course TIF25A',
    description: 'Course group',
    type: 'Course',
    audience: 'TIF25A',
    courseCode: 'TIF25A',
    ownerUserId: null,
    ownerLabel: 'Computer Science',
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
    groupsService = {
      getGroups: vi.fn(() => of([group])),
      createGroup: vi.fn(() => of({ ...group, id: 'group-2', type: 'Social', canManage: true })),
      joinGroup: vi.fn(() => of({ ...group, isAssigned: true, canJoin: false })),
    };

    await TestBed.configureTestingModule({
      imports: [GroupsPage],
      providers: [
        provideRouter([]),
        {
          provide: Groups,
          useValue: groupsService,
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

  it('filters visible groups by search text', () => {
    (component as any)._searchQuery.set('library');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('There are no groups for this filter yet.');
  });

  it('shows joinable public groups in explore tab', () => {
    fixture.detectChanges();
    const exploreGroup: CampusGroup = { ...group, id: 'group-3', type: 'Social', isAssigned: false, canPost: false, canJoin: true };
    (component as any)._groups.set([group, exploreGroup]);
    (component as any)._activeTab.set('Explore');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Join');
  });

  it('sends selected group type when creating a group', () => {
    (component as any)._isCreateMenuOpen.set(true);
    (component as any)._createType.set('Official');
    (component as any)._createName.set('Exam office');
    (component as any)._createDescription.set('Exam information');
    (component as any)._createAudience.set('All students');

    (component as any).createGroup();

    expect(groupsService.createGroup).toHaveBeenCalledWith(expect.objectContaining({ type: 'Official', courseCode: null }));
  });
});
