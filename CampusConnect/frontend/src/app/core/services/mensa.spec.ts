import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Mensa } from './mensa';

describe('Mensa', () => {
  let service: Mensa;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(Mensa);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should load the week menu from the backend API', () => {
    service.getWeekMenu().subscribe(response => {
      expect(response).toEqual([{ date: '2026-04-28', dishes: [] }]);
    });

    const request = http.expectOne('/api/mensa');
    expect(request.request.method).toBe('GET');
    request.flush([{ date: '2026-04-28', dishes: [] }]);
  });
});
