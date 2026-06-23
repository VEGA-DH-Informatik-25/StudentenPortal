import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { GroupsPage } from './groups-page';
import { Auth } from '../../../core/services/auth';
import { Groups } from '../../../core/services/groups';
import { CampusGroup } from '../../../core/models/group.model';

describe('GroupsPage', () => {
  let component: GroupsPage;
  let fixture: ComponentFixture<GroupsPage>;
  let userRole: ReturnType<typeof vi.fn>;
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
    userRole = vi.fn(() => 'Student');
    groupsService = {
      getGroups: vi.fn(() => of([group])),
      createGroup: vi.fn(() => of({ ...group, id: 'group-2', type: 'Campus', canManage: true })),
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
        {
          provide: Auth,
          useValue: { userRole },
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

    expect(fixture.nativeElement.textContent).toContain('Für diesen Filter gibt es noch keine Gruppen.');
  });

  it('shows joinable public groups in explore tab', () => {
    fixture.detectChanges();
    const exploreGroup: CampusGroup = { ...group, id: 'group-3', type: 'Campus', isAssigned: false, canPost: false, canJoin: true };
    (component as any)._groups.set([group, exploreGroup]);
    (component as any)._activeTab.set('Explore');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Beitreten');
  });

  it('shows only campus group creation to students', () => {
    (component as any)._isCreateMenuOpen.set(true);
    fixture.detectChanges();

    const typeCards = Array.from(
      fixture.nativeElement.querySelectorAll('.create-type-card') as NodeListOf<Element>
    ).map(element => element.textContent?.trim());

    expect(typeCards).toHaveLength(1);
    expect(typeCards[0]).toContain('Campusgruppe');
  });

  it('shows course creation to lecturers but not official creation', () => {
    userRole.mockReturnValue('Lecturer');
    (component as any)._isCreateMenuOpen.set(true);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Kursgruppe');
    expect(text).not.toContain('Offizielle Gruppe');
  });

  it('does not submit a group type forbidden for the current role', () => {
    (component as any)._createType.set('Official');
    (component as any)._createName.set('Exam office');
    (component as any)._createDescription.set('Exam information');
    (component as any)._createAudience.set('All students');
    (component as any)._createOfficialCategory.set('Exam office');

    (component as any).createGroup();

    expect(groupsService.createGroup).not.toHaveBeenCalled();
  });

  it('sends an allowed selected group type when creating a group', () => {
    userRole.mockReturnValue('Management');
    (component as any)._createType.set('Official');
    (component as any)._createName.set('Exam office');
    (component as any)._createDescription.set('Exam information');
    (component as any)._createAudience.set('All students');
    (component as any)._createOfficialCategory.set('Exam office');

    (component as any).createGroup();

    expect(groupsService.createGroup).toHaveBeenCalledWith(expect.objectContaining({ type: 'Official', courseCode: null, officialCategory: 'Exam office' }));
  });

  it('keeps request-based groups in the explore tab', () => {
    fixture.detectChanges();
    const requestGroup: CampusGroup = {
      ...group,
      id: 'group-request',
      type: 'Campus',
      isAssigned: false,
      canPost: false,
      canJoin: false,
      canRequestJoin: true,
      groupRole: 'None',
    };
    (component as any)._groups.set([group, requestGroup]);
    (component as any)._activeTab.set('Explore');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Beitritt anfragen');
  });
});
