import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';

import { Timetable } from './timetable';

describe('Timetable', () => {
  let service: Timetable;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(Timetable);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should request the full supported timetable window by default', () => {
    service.getTimetable('tif25a').subscribe(response => {
      expect(response.course).toBe('TIF25A');
    });

    const request = http.expectOne(req => req.url === '/api/timetable');
    expect(request.request.params.get('course')).toBe('TIF25A');
    expect(request.request.params.get('days')).toBe('120');

    request.flush({ course: 'TIF25A', timezone: 'Europe/Berlin', days: [] });
  });
});
