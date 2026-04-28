import { Component } from '@angular/core';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { Auth } from '../services/auth';
import { errorHandlerInterceptor } from './error-handler-interceptor';

describe('errorHandlerInterceptor', () => {
  let httpClient: HttpClient;
  let http: HttpTestingController;
  let auth: { logout: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    auth = { logout: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'login', component: TestLoginComponent }]),
        provideHttpClient(withInterceptors([errorHandlerInterceptor])),
        provideHttpClientTesting(),
        { provide: Auth, useValue: auth },
      ],
    });
    httpClient = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should log out the user on unauthorized API responses', () => {
    httpClient.get('/api/protected').subscribe({ error: () => undefined });

    const request = http.expectOne('/api/protected');
    request.flush({ error: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(auth.logout).toHaveBeenCalled();
  });
});

@Component({ template: '' })
class TestLoginComponent {}
