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
    if (req.attachments?.length || req.translations) {
      const form = new FormData();
      form.append('content', req.content);
      if (req.groupId) {
        form.append('groupId', req.groupId);
      }
      form.append('allowComments', String(req.allowComments ?? true));
      if (req.translations) {
        form.append('translations.de', req.translations.de);
        form.append('translations.en', req.translations.en);
        form.append('translations.fr', req.translations.fr);
      }
      for (const attachment of req.attachments ?? []) {
        form.append('attachments', attachment, attachment.name);
      }

      return this._http.post<FeedPost>('/api/feed', form);
    }

    return this._http.post<FeedPost>('/api/feed', req);
  }

  getPendingPosts(groupId: string): Observable<FeedPost[]> {
    return this._http.get<FeedPost[]>(`/api/groups/${groupId}/pending-posts`);
  }

  approvePost(id: string): Observable<FeedPost> {
    return this._http.post<FeedPost>(`/api/feed/${id}/approve`, {});
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

