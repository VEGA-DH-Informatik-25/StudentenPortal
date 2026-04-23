import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Feed } from '../../../core/services/feed';
import { FeedPost } from '../../../core/models/feed.model';

@Component({
  selector: 'app-feed-page',
  imports: [FormsModule, DatePipe],
  templateUrl: './feed-page.html',
  styleUrl: './feed-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeedPage implements OnInit {
  private readonly _feedService = inject(Feed);

  protected readonly _posts = signal<FeedPost[]>([]);
  protected readonly _isLoading = signal(false);
  protected readonly _error = signal('');
  protected readonly _newContent = signal('');

  ngOnInit(): void {
    this._loadFeed();
  }

  private _loadFeed(): void {
    this._isLoading.set(true);
    this._feedService.getFeed().subscribe({
      next: posts => { this._posts.set(posts); this._isLoading.set(false); },
      error: () => { this._error.set('Feed konnte nicht geladen werden.'); this._isLoading.set(false); },
    });
  }

  protected onPost(): void {
    const content = this._newContent().trim();
    if (!content) return;
    this._feedService.createPost({ content }).subscribe({
      next: post => { this._posts.update(p => [post, ...p]); this._newContent.set(''); },
      error: () => this._error.set('Beitrag konnte nicht erstellt werden.'),
    });
  }

  protected onDelete(id: string): void {
    this._feedService.deletePost(id).subscribe({
      next: () => this._posts.update(p => p.filter(x => x.id !== id)),
    });
  }

  protected updateContent(value: string): void {
    this._newContent.set(value);
  }
}

