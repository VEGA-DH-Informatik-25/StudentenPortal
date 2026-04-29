import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { Feed } from './feed';

describe('Feed', () => {
  let service: Feed;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(Feed);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should send the selected group when creating a post', () => {
    service.createPost({ content: 'Hallo Kurs', groupId: 'group-1' }).subscribe();

    const request = http.expectOne('/api/feed');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ content: 'Hallo Kurs', groupId: 'group-1' });
    request.flush({});
  });
});
