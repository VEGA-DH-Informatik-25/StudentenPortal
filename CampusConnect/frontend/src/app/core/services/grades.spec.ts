import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Grades } from './grades';

describe('Grades', () => {
  let service: Grades;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(Grades);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should load the current grade summary', () => {
    service.getGrades().subscribe(response => {
      expect(response.totalEcts).toBe(5);
    });

    const request = http.expectOne('/api/grades');
    expect(request.request.method).toBe('GET');
    request.flush({ grades: [], weightedAverage: 0, totalEcts: 5 });
  });

  it('should add a grade through the API', () => {
    service.addGrade({ moduleName: 'Mathematik', value: 1.7, ects: 5 }).subscribe();

    const request = http.expectOne('/api/grades');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ moduleName: 'Mathematik', value: 1.7, ects: 5 });
    request.flush({ id: 'grade-1', moduleName: 'Mathematik', value: 1.7, ects: 5, createdAt: '2026-04-28T10:00:00Z' });
  });

  it('should delete a grade through the API', () => {
    service.deleteGrade('grade-1').subscribe();

    const request = http.expectOne('/api/grades/grade-1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
