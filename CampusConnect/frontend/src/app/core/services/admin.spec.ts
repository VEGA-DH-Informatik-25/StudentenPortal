import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Admin } from './admin';

describe('Admin', () => {
  let service: Admin;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(Admin);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should create courses through the admin endpoint', () => {
    service.createCourse({ code: 'TIF25A', studyProgram: 'Informatik', semester: 1 }).subscribe();

    const request = http.expectOne('/api/admin/courses');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ code: 'TIF25A', studyProgram: 'Informatik', semester: 1 });
    request.flush({ code: 'TIF25A', studyProgram: 'Informatik', semester: 1, isActive: true, createdAt: '2026-04-28T10:00:00Z' });
  });

  it('should update user roles with a patch request', () => {
    service.updateUserRole('user-1', 'Lecturer').subscribe();

    const request = http.expectOne('/api/admin/users/user-1/role');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ role: 'Lecturer' });
    request.flush({});
  });

  it('should delete users through the admin endpoint', () => {
    service.deleteUser('user-1').subscribe();

    const request = http.expectOne('/api/admin/users/user-1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});