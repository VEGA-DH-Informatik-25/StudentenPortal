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
    service.addGrade({ moduleCode: 'T4INF1001', value: 1.7 }).subscribe();

    const request = http.expectOne('/api/grades');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ moduleCode: 'T4INF1001', value: 1.7 });
    request.flush({ id: 'grade-1', moduleCode: 'T4INF1001', moduleName: 'Mathematik I', value: 1.7, ects: 5, createdAt: '2026-04-28T10:00:00Z' });
  });

  it('should load the current course grade plan', () => {
    service.getGradePlan().subscribe(response => {
      expect(response.courseCode).toBe('TIF25A');
      expect(response.modules.length).toBe(1);
    });

    const request = http.expectOne('/api/grades/plan');
    expect(request.request.method).toBe('GET');
    request.flush({
      courseCode: 'TIF25A',
      studyProgram: 'Informatik',
      sourceUrl: 'https://example.invalid/Informatik.pdf',
      retrievedAt: '2026-04-29T10:00:00Z',
      modules: [{ code: 'T4INF1001', name: 'Mathematik I', studyYear: 1, ects: 5, isRequired: true, isCompleted: false, grade: null, exams: [] }],
    });
  });

  it('should delete a grade through the API', () => {
    service.deleteGrade('grade-1').subscribe();

    const request = http.expectOne('/api/grades/grade-1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
