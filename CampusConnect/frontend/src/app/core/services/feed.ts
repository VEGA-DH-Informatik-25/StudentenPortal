import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateCommentRequest, CreatePostRequest, FeedPost, ToggleReactionRequest } from '../models/feed.model';

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

  createComment(postId: string, req: CreateCommentRequest): Observable<FeedPost> {
    return this._http.post<FeedPost>(`/api/feed/${postId}/comments`, req);
  }

  deleteComment(postId: string, commentId: string): Observable<FeedPost> {
    return this._http.delete<FeedPost>(`/api/feed/${postId}/comments/${commentId}`);
  }

  toggleReaction(postId: string, req: ToggleReactionRequest): Observable<FeedPost> {
    return this._http.post<FeedPost>(`/api/feed/${postId}/reactions`, req);
  }
}

