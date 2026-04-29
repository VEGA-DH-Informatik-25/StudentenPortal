import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { Contacts } from './contacts';

describe('Contacts', () => {
  let service: Contacts;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(Contacts);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should search contacts with trimmed query text', () => {
    service.searchContacts('  TIF25A  ').subscribe();

    const request = http.expectOne('/api/contacts?query=TIF25A');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });
});
