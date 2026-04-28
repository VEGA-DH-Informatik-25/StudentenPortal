import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Courses } from './courses';

describe('Courses', () => {
  let service: Courses;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(Courses);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should load active courses from the public courses endpoint', () => {
    service.getCourses().subscribe(response => {
      expect(response[0].code).toBe('TIF25A');
    });

    const request = http.expectOne('/api/courses');
    expect(request.request.method).toBe('GET');
    request.flush([{ code: 'TIF25A', studyProgram: 'Informatik', semester: 1, isActive: true, createdAt: '2026-04-28T10:00:00Z' }]);
  });
});
