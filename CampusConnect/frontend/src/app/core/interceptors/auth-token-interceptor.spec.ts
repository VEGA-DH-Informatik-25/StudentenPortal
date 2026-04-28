import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Auth } from '../services/auth';
import { authTokenInterceptor } from './auth-token-interceptor';

describe('authTokenInterceptor', () => {
  let token: string | null;
  let httpClient: HttpClient;
  let http: HttpTestingController;

  beforeEach(() => {
    token = null;
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authTokenInterceptor])),
        provideHttpClientTesting(),
        { provide: Auth, useValue: { getToken: () => token } },
      ],
    });
    httpClient = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should attach bearer tokens when available', () => {
    token = 'jwt-token';

    httpClient.get('/api/protected').subscribe();

    const request = http.expectOne('/api/protected');
    expect(request.request.headers.get('Authorization')).toBe('Bearer jwt-token');
    request.flush({});
  });

  it('should leave requests unchanged when no token exists', () => {
    httpClient.get('/api/public').subscribe();

    const request = http.expectOne('/api/public');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });
});
