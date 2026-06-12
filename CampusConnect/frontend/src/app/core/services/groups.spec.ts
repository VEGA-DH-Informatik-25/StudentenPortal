import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { Groups } from './groups';

describe('Groups', () => {
  let service: Groups;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(Groups);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should update settings for a group', () => {
    service.updateSettings('group-1', {
      allowStudentPosts: true,
      allowComments: false,
      requiresApproval: true,
      isDiscoverable: true,
      joinRule: 'Open',
    }).subscribe();

    const request = http.expectOne('/api/groups/group-1/settings');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      allowStudentPosts: true,
      allowComments: false,
      requiresApproval: true,
      isDiscoverable: true,
      joinRule: 'Open',
    });
    request.flush({});
  });

  it('should create a group', () => {
    service.createGroup({
      name: 'Lerngruppe Web',
      description: 'Gemeinsam lernen',
      type: 'Campus',
      audience: 'Interessierte',
      courseCode: null,
      officialCategory: null,
      allowStudentPosts: true,
      allowComments: true,
      requiresApproval: false,
      isDiscoverable: true,
      joinRule: 'Open',
    }).subscribe();

    const request = http.expectOne('/api/groups');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      name: 'Lerngruppe Web',
      description: 'Gemeinsam lernen',
      type: 'Campus',
      audience: 'Interessierte',
      courseCode: null,
      officialCategory: null,
      allowStudentPosts: true,
      allowComments: true,
      requiresApproval: false,
      isDiscoverable: true,
      joinRule: 'Open',
    });
    request.flush({});
  });

  it('should load settings details for a group', () => {
    service.getSettings('group-1').subscribe();

    const request = http.expectOne('/api/groups/group-1/settings');
    expect(request.request.method).toBe('GET');
    request.flush({});
  });

  it('should delete a group', () => {
    service.deleteGroup('group-1').subscribe();

    const request = http.expectOne('/api/groups/group-1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });

  it('should search candidates for a group', () => {
    service.searchCandidates('group-1', 'bob').subscribe();

    const request = http.expectOne('/api/groups/group-1/candidates?query=bob');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('should add members to a group', () => {
    service.addMembers('group-1', { userIds: ['user-1', 'user-2'] }).subscribe();

    const request = http.expectOne('/api/groups/group-1/members');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ userIds: ['user-1', 'user-2'] });
    request.flush({});
  });

  it('should add a whole course to a group', () => {
    service.addCourse('group-1', { courseCode: 'TIF25A' }).subscribe();

    const request = http.expectOne('/api/groups/group-1/members/course');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ courseCode: 'TIF25A' });
    request.flush({});
  });

  it('should remove a member from a group', () => {
    service.removeMember('group-1', 'user-2').subscribe();

    const request = http.expectOne('/api/groups/group-1/members/user-2');
    expect(request.request.method).toBe('DELETE');
    request.flush({});
  });

  it('should set a member role', () => {
    service.setMemberRole('group-1', 'user-2', { role: 'Moderator' }).subscribe();

    const request = http.expectOne('/api/groups/group-1/members/user-2/role');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ role: 'Moderator' });
    request.flush({});
  });
});
