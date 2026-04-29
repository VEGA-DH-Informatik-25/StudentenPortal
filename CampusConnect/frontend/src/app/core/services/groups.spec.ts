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
    }).subscribe();

    const request = http.expectOne('/api/groups/group-1/settings');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      allowStudentPosts: true,
      allowComments: false,
      requiresApproval: true,
      isDiscoverable: true,
    });
    request.flush({});
  });

  it('should create a group', () => {
    service.createGroup({
      name: 'Lerngruppe Web',
      description: 'Gemeinsam lernen',
      audience: 'Interessierte',
      allowStudentPosts: true,
      allowComments: true,
      requiresApproval: false,
      isDiscoverable: true,
    }).subscribe();

    const request = http.expectOne('/api/groups');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      name: 'Lerngruppe Web',
      description: 'Gemeinsam lernen',
      audience: 'Interessierte',
      allowStudentPosts: true,
      allowComments: true,
      requiresApproval: false,
      isDiscoverable: true,
    });
    request.flush({});
  });

  it('should load settings details for a group', () => {
    service.getSettings('group-1').subscribe();

    const request = http.expectOne('/api/groups/group-1/settings');
    expect(request.request.method).toBe('GET');
    request.flush({});
  });

  it('should update group assignments', () => {
    service.updateAssignments('group-1', { userIds: ['user-1', 'user-2'] }).subscribe();

    const request = http.expectOne('/api/groups/group-1/assignments');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ userIds: ['user-1', 'user-2'] });
    request.flush({});
  });
});
