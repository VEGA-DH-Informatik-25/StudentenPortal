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
    service.createPost({ content: 'Hello course', groupId: 'group-1', allowComments: false }).subscribe();

    const request = http.expectOne('/api/feed');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ content: 'Hello course', groupId: 'group-1', allowComments: false });
    request.flush({});
  });

  it('should send multipart form data for translated posts with attachments', () => {
    const file = new File(['hello'], 'notice.pdf', { type: 'application/pdf' });

    service.createPost({
      content: 'Hallo',
      groupId: 'group-1',
      allowComments: true,
      translations: { de: 'Hallo', en: 'Hello', fr: 'Bonjour' },
      attachments: [file],
    }).subscribe();

    const request = http.expectOne('/api/feed');
    expect(request.request.method).toBe('POST');
    expect(request.request.body instanceof FormData).toBe(true);
    const body = request.request.body as FormData;
    expect(body.get('content')).toBe('Hallo');
    expect(body.get('groupId')).toBe('group-1');
    expect(body.get('translations.de')).toBe('Hallo');
    expect(body.get('translations.en')).toBe('Hello');
    expect(body.get('translations.fr')).toBe('Bonjour');
    expect(body.get('attachments')).toBe(file);
    request.flush({});
  });

  it('should load and approve pending posts', () => {
    service.getPendingPosts('group-1').subscribe();
    const pending = http.expectOne('/api/groups/group-1/pending-posts');
    expect(pending.request.method).toBe('GET');
    pending.flush([]);

    service.approvePost('post-1').subscribe();
    const approve = http.expectOne('/api/feed/post-1/approve');
    expect(approve.request.method).toBe('POST');
    approve.flush({});
  });
});
