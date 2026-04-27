import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FeedPost, CreatePostRequest } from '../models/feed.model';

@Injectable({ providedIn: 'root' })
export class Feed {
  private readonly _http = inject(HttpClient);

  getFeed(page = 1): Observable<FeedPost[]> {
    return this._http.get<FeedPost[]>(`/api/feed?page=${page}&pageSize=20`);
  }

  createPost(req: CreatePostRequest): Observable<FeedPost> {
    return this._http.post<FeedPost>('/api/feed', req);
  }

  deletePost(id: string): Observable<void> {
    return this._http.delete<void>(`/api/feed/${id}`);
  }
}

