import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Calendar } from './calendar';

describe('Calendar', () => {
  let service: Calendar;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(Calendar);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should create exam entries through the API', () => {
    const requestBody = {
      moduleName: 'Mathematik',
      examDate: '2026-05-10T08:00:00.000Z',
      location: 'Aula',
      notes: 'Taschenrechner',
    };

    service.addExam(requestBody).subscribe();

    const request = http.expectOne('/api/calendar');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(requestBody);
    request.flush({ id: 'exam-1', ...requestBody });
  });

  it('should delete exam entries through the API', () => {
    service.deleteExam('exam-1').subscribe();

    const request = http.expectOne('/api/calendar/exam-1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
