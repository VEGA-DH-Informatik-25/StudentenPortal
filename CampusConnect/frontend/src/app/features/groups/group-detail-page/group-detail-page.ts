import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';

import { FeedPost } from '../../../core/models/feed.model';
import { CampusGroup, GroupType } from '../../../core/models/group.model';
import { Auth } from '../../../core/services/auth';
import { Feed } from '../../../core/services/feed';
import { Groups } from '../../../core/services/groups';

@Component({
  selector: 'app-group-detail-page',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './group-detail-page.html',
  styleUrl: './group-detail-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupDetailPage implements OnInit {
  private readonly _auth = inject(Auth);
  private readonly _feedService = inject(Feed);
  private readonly _groupsService = inject(Groups);
  private readonly _route = inject(ActivatedRoute);
  private readonly _router = inject(Router);

  protected readonly _group = signal<CampusGroup | null>(null);
  protected readonly _posts = signal<FeedPost[]>([]);
  protected readonly _isLoading = signal(false);
  protected readonly _isJoining = signal(false);
  protected readonly _error = signal('');
  protected readonly _newContent = signal('');
  protected readonly _commentDrafts = signal<Record<string, string>>({});
  protected readonly _openCommentPostIds = signal<string[]>([]);
  protected readonly _commentingPostIds = signal<string[]>([]);
  protected readonly _openReactionPostId = signal<string | null>(null);
  protected readonly _reactingKeys = signal<string[]>([]);
  protected readonly _postCountLabel = computed(() => this._posts().length === 1 ? '1 Beitrag' : `${this._posts().length} Beiträge`);
  protected readonly _displayName = computed(() => this._auth.displayName() || 'Studierende');
  protected readonly _profileInitials = computed(() => this.initialsFor(this._displayName()));
  protected readonly _emojiOptions = ['👍', '❤️', '🙌', '👏', '🎉', '🔥', '💡', '✅', '🚀', '👀', '🙂', '😂', '😮', '🤔', '🙏', '💪', '📌', '⭐', '☕', '🍀'];

  ngOnInit(): void {
    const groupId = this._route.snapshot.paramMap.get('id');
    if (!groupId) {
      this._error.set('Gruppe wurde nicht gefunden.');
      return;
    }

    this._loadGroup(groupId);
  }

  protected backToGroups(): void {
    void this._router.navigate(['/groups']);
  }

  protected openSettings(group: CampusGroup): void {
    if (!group.canManage) {
      return;
    }

    void this._router.navigate(['/groups', group.id, 'settings']);
  }

  protected joinGroup(group: CampusGroup): void {
    if (!group.canJoin || this._isJoining()) {
      return;
    }

    this._isJoining.set(true);
    this._error.set('');
    this._groupsService.joinGroup(group.id).subscribe({
      next: updatedGroup => {
        this._group.set(updatedGroup);
        this._isJoining.set(false);
        this._loadGroup(updatedGroup.id);
      },
      error: () => {
        this._error.set('Beitritt konnte nicht gespeichert werden.');
        this._isJoining.set(false);
      },
    });
  }

  protected onPost(): void {
    const group = this._group();
    const content = this._newContent().trim();
    if (!group || !group.canPost || !content) {
      return;
    }

    this._error.set('');
    this._feedService.createPost({ content, groupId: group.id }).subscribe({
      next: post => {
        if (post.group.id === group.id) {
          this._posts.update(posts => [post, ...posts]);
        }
        this._newContent.set('');
      },
      error: () => this._error.set('Beitrag konnte nicht erstellt werden.'),
    });
  }

  protected onDelete(id: string): void {
    this._feedService.deletePost(id).subscribe({
      next: () => this._posts.update(posts => posts.filter(post => post.id !== id)),
      error: () => this._error.set('Beitrag konnte nicht gelöscht werden.'),
    });
  }

  protected onComment(post: FeedPost): void {
    const content = this.commentDraft(post.id).trim();
    if (!content || !post.canComment || this.isCommenting(post.id)) {
      return;
    }

    this._commentingPostIds.update(ids => [...ids, post.id]);
    this._error.set('');
    this._feedService.createComment(post.id, { content }).subscribe({
      next: updatedPost => {
        this._replacePost(updatedPost);
        this.updateCommentDraft(post.id, '');
        this._openCommentPostIds.update(ids => ids.filter(id => id !== post.id));
        this._commentingPostIds.update(ids => ids.filter(id => id !== post.id));
      },
      error: () => {
        this._error.set('Kommentar konnte nicht gespeichert werden.');
        this._commentingPostIds.update(ids => ids.filter(id => id !== post.id));
      },
    });
  }

  protected onDeleteComment(postId: string, commentId: string): void {
    this._feedService.deleteComment(postId, commentId).subscribe({
      next: updatedPost => this._replacePost(updatedPost),
      error: () => this._error.set('Kommentar konnte nicht gelöscht werden.'),
    });
  }

  protected onToggleReaction(post: FeedPost, emoji: string): void {
    emoji = emoji.trim();
    if (!emoji || !this.canReactToPost(post)) {
      return;
    }

    const key = this.reactionKey(post.id, emoji);
    if (this._reactingKeys().includes(key)) {
      return;
    }

    this._reactingKeys.update(keys => [...keys, key]);
    this._error.set('');
    this._feedService.toggleReaction(post.id, { emoji }).subscribe({
      next: updatedPost => {
        this._replacePost(updatedPost);
        if (this._openReactionPostId() === post.id) {
          this._openReactionPostId.set(null);
        }
        this._reactingKeys.update(keys => keys.filter(item => item !== key));
      },
      error: () => {
        this._error.set('Reaktion konnte nicht gespeichert werden.');
        this._reactingKeys.update(keys => keys.filter(item => item !== key));
      },
    });
  }

  protected updateContent(value: string): void {
    this._newContent.set(value);
  }

  protected updateCommentDraft(postId: string, value: string): void {
    this._commentDrafts.update(drafts => ({ ...drafts, [postId]: value }));
  }

  protected toggleCommentComposer(post: FeedPost): void {
    if (!post.canComment) {
      return;
    }

    this._openCommentPostIds.update(ids => ids.includes(post.id)
      ? ids.filter(id => id !== post.id)
      : [...ids, post.id]);
  }

  protected isCommentComposerOpen(postId: string): boolean {
    return this._openCommentPostIds().includes(postId);
  }

  protected toggleReactionMenu(post: FeedPost): void {
    if (!this.canReactToPost(post)) {
      return;
    }

    this._openReactionPostId.update(openId => openId === post.id ? null : post.id);
  }

  protected isReactionMenuOpen(postId: string): boolean {
    return this._openReactionPostId() === postId;
  }

  protected onPickReaction(post: FeedPost, emoji: string): void {
    this.onToggleReaction(post, emoji);
  }

  protected commentDraft(postId: string): string {
    return this._commentDrafts()[postId] ?? '';
  }

  protected reactionCount(post: FeedPost, emoji: string): number {
    return post.reactions.find(reaction => reaction.emoji === emoji)?.count ?? 0;
  }

  protected hasReacted(post: FeedPost, emoji: string): boolean {
    return post.reactions.find(reaction => reaction.emoji === emoji)?.reactedByCurrentUser ?? false;
  }

  protected reactionKey(postId: string, emoji: string): string {
    return `${postId}:${emoji}`;
  }

  protected isReacting(postId: string, emoji: string): boolean {
    return this._reactingKeys().includes(this.reactionKey(postId, emoji));
  }

  protected isCommenting(postId: string): boolean {
    return this._commentingPostIds().includes(postId);
  }

  protected canReactToPost(post: FeedPost): boolean {
    return post.group.canManage || (post.group.isAssigned && post.group.memberPermission === 'ReadWrite');
  }

  protected groupTypeLabel(type: GroupType): string {
    switch (type) {
      case 'Course':
        return 'Kursgruppe';
      case 'Official':
        return 'Offizielle Gruppe';
      case 'Social':
        return 'Campusgruppe';
    }
  }

  protected permissionLabel(group: CampusGroup): string {
    if (!group.isAssigned && !group.canManage) {
      return group.canJoin ? 'Öffentlich auffindbar' : 'Nicht zugewiesen';
    }

    if (group.memberPermission === 'Manage' || group.canManage) {
      return 'Verwalten';
    }

    return group.memberPermission === 'ReadWrite' ? 'Lesen & Schreiben' : 'Nur lesen';
  }

  protected commentPolicyLabel(group: CampusGroup): string {
    return group.settings.allowComments ? 'Kommentare offen' : 'Kommentare geschlossen';
  }

  protected initialsFor(value: string): string {
    return value
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map(part => part[0]?.toUpperCase() ?? '')
      .join('') || 'CC';
  }

  private _loadGroup(groupId: string): void {
    this._isLoading.set(true);
    this._error.set('');

    forkJoin({ groups: this._groupsService.getGroups(), posts: this._feedService.getFeed() }).subscribe({
      next: ({ groups, posts }) => {
        const group = groups.find(item => item.id === groupId) ?? null;
        this._group.set(group);
        this._posts.set(group ? posts.filter(post => post.group.id === group.id) : []);
        this._openReactionPostId.set(null);
        this._openCommentPostIds.set([]);
        this._error.set(group ? '' : 'Gruppe wurde nicht gefunden oder ist für dich nicht sichtbar.');
        this._isLoading.set(false);
      },
      error: () => {
        this._group.set(null);
        this._posts.set([]);
        this._error.set('Gruppenbeiträge konnten nicht geladen werden.');
        this._isLoading.set(false);
      },
    });
  }

  private _replacePost(updatedPost: FeedPost): void {
    const group = this._group();
    if (!group || updatedPost.group.id !== group.id) {
      return;
    }

    this._posts.update(posts => posts.map(post => post.id === updatedPost.id ? updatedPost : post));
  }
}
