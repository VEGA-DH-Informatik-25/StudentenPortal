import { Component, ChangeDetectionStrategy, HostListener, computed, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { I18n } from '../../../core/i18n/i18n';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Auth } from '../../../core/services/auth';
import { Feed } from '../../../core/services/feed';
import { FeedAttachment, FeedPost } from '../../../core/models/feed.model';
import { CampusGroup, GroupType } from '../../../core/models/group.model';
import { Groups } from '../../../core/services/groups';
import { TimetableEvent } from '../../../core/models/timetable.model';
import { Timetable } from '../../../core/services/timetable';
import { UiPreferences } from '../../../core/services/ui-preferences';
import { ProfileHoverCard } from '../../../shared/ui/profile-hover-card/profile-hover-card';

const FEED_SELECTED_GROUP_KEY = 'campusconnect.feed.selectedGroupId';
const MAX_ATTACHMENT_COUNT = 5;
const MAX_ATTACHMENT_BYTES = 10 * 1024 * 1024;
const ALLOWED_ATTACHMENT_EXTENSIONS = ['jpg', 'jpeg', 'png', 'webp', 'gif', 'pdf', 'txt', 'csv', 'doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx'];

@Component({
  selector: 'app-feed-page',
  standalone: true,
  imports: [FormsModule, DatePipe, ProfileHoverCard, TranslatePipe],
  templateUrl: './feed-page.html',
  styleUrl: './feed-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeedPage implements OnInit {
  private readonly _auth = inject(Auth);
  private readonly _feedService = inject(Feed);
  private readonly _groupsService = inject(Groups);
  protected readonly _i18n = inject(I18n);
  private readonly _router = inject(Router);
  private readonly _timetableService = inject(Timetable);
  private readonly _uiPreferences = inject(UiPreferences);

  protected readonly _posts = signal<FeedPost[]>([]);
  protected readonly _isLoading = signal(false);
  protected readonly _isPosting = signal(false);
  protected readonly _error = signal('');
  protected readonly _notice = signal('');
  protected readonly _isComposerOpen = signal(false);
  protected readonly _isComposerSettingsOpen = signal(false);
  protected readonly _newContent = signal('');
  protected readonly _useTranslations = signal(false);
  protected readonly _translationDraft = signal({ de: '', en: '', fr: '' });
  protected readonly _selectedFiles = signal<File[]>([]);
  protected readonly _attachmentError = signal('');
  protected readonly _allowComments = signal(true);
  protected readonly _commentDrafts = signal<Record<string, string>>({});
  protected readonly _openCommentPostIds = signal<string[]>([]);
  protected readonly _commentingPostIds = signal<string[]>([]);
  protected readonly _openReactionPostId = signal<string | null>(null);
  protected readonly _reactingKeys = signal<string[]>([]);
  protected readonly _previewAttachment = signal<FeedAttachment | null>(null);
  protected readonly _groups = signal<CampusGroup[]>([]);
  protected readonly _groupsLoading = signal(false);
  protected readonly _groupsError = signal('');
  protected readonly _selectedGroupId = signal(this._uiPreferences.getString(FEED_SELECTED_GROUP_KEY));
  protected readonly _postableGroups = computed(() => this._groups().filter(group => this.canPostToGroup(group)));
  protected readonly _selectedGroup = computed<CampusGroup | null>(() => {
    const selectedId = this._selectedGroupId();
    return this._postableGroups().find(group => group.id === selectedId) ?? this._postableGroups()[0] ?? null;
  });
  protected readonly _displayName = computed(() => this._auth.displayName() || this._i18n.roleLabel('Student'));
  protected readonly _profileInitials = computed(() => this._initialsFor(this._displayName()));
  protected readonly _emojiOptions = ['👍', '❤️', '😂', '🎉', '🙌', '🚀', '👀', '🙏'];
  protected readonly _scheduleEvents = signal<TimetableEvent[]>([]);
  protected readonly _scheduleCourse = signal('');
  protected readonly _scheduleDate = signal(this._dateKey(new Date()));
  protected readonly _scheduleTimezone = signal('Europe/Berlin');
  protected readonly _scheduleIsLoading = signal(false);
  protected readonly _scheduleError = signal('');
  protected readonly _scheduleTitle = computed(() => this._formatDateLong(this._scheduleDate()));
  protected readonly _canSubmitPost = computed(() => {
    const hasText = this._useTranslations()
      ? Object.values(this._translationDraft()).every(value => value.trim())
      : !!this._newContent().trim();
    return hasText && !this._isLoading() && !this._isPosting() && !!this._selectedGroup() && !this._attachmentError();
  });
  protected readonly _composerCharacterCount = computed(() =>
    this._useTranslations() ? this._translationDraft().de.trim().length : this._newContent().trim().length
  );

  ngOnInit(): void {
    this._loadGroups();
    this._loadFeed();
    this._loadTodaySchedule();
  }

  private _loadFeed(): void {
    this._isLoading.set(true);
    this._error.set('');
    this._feedService.getFeed().subscribe({
      next: posts => { this._posts.set(posts); this._error.set(''); this._isLoading.set(false); },
      error: error => { this._error.set(this._i18n.readError(error, 'feed.loadError')); this._isLoading.set(false); },
    });
  }

  protected onPost(): void {
    const translations = this._useTranslations()
      ? {
        de: this._translationDraft().de.trim(),
        en: this._translationDraft().en.trim(),
        fr: this._translationDraft().fr.trim(),
      }
      : null;
    const content = translations?.de ?? this._newContent().trim();
    if (!this._canSubmitPost() || this._isPosting()) return;
    const group = this._selectedGroup();
    if (!group) {
      this._error.set(this._i18n.translate('feed.selectGroupFirst'));
      return;
    }

    this._error.set('');
    this._notice.set('');
    this._isPosting.set(true);
    this._feedService.createPost({
      content,
      groupId: group.id,
      allowComments: group.settings.allowComments && this._allowComments(),
      translations,
      attachments: this._selectedFiles(),
    }).subscribe({
      next: post => {
        if (post.status === 'Published') {
          this._posts.update(posts => [post, ...posts]);
        } else {
          this._notice.set(this._i18n.translate('feed.pendingNotice'));
        }
        this._newContent.set('');
        this._translationDraft.set({ de: '', en: '', fr: '' });
        this._useTranslations.set(false);
        this._selectedFiles.set([]);
        this._attachmentError.set('');
        this._allowComments.set(true);
        this._isComposerOpen.set(false);
        this._isComposerSettingsOpen.set(false);
        this._isPosting.set(false);
      },
      error: error => {
        this._error.set(this._i18n.readError(error, 'feed.createPostError'));
        this._isComposerOpen.set(true);
        this._isPosting.set(false);
      },
    });
  }

  protected onDelete(id: string): void {
    if (!globalThis.confirm(this._i18n.translate('feed.confirmDeletePost'))) {
      return;
    }

    this._error.set('');
    this._feedService.deletePost(id).subscribe({
      next: () => this._posts.update(posts => posts.filter(post => post.id !== id)),
      error: error => this._error.set(this._i18n.readError(error, 'feed.deletePostError')),
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
      error: error => {
        this._error.set(this._i18n.readError(error, 'feed.createCommentError'));
        this._commentingPostIds.update(ids => ids.filter(id => id !== post.id));
      },
    });
  }

  protected onDeleteComment(postId: string, commentId: string): void {
    this._feedService.deleteComment(postId, commentId).subscribe({
      next: updatedPost => this._replacePost(updatedPost),
      error: error => this._error.set(this._i18n.readError(error, 'feed.deleteCommentError')),
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
      error: error => {
        this._error.set(this._i18n.readError(error, 'feed.saveReactionError'));
        this._reactingKeys.update(keys => keys.filter(item => item !== key));
      },
    });
  }

  protected openComposer(): void {
    this._isComposerOpen.set(true);
  }

  protected openAttachmentPreview(attachment: FeedAttachment): void {
    if (!attachment.isImage) {
      return;
    }

    this._previewAttachment.set(attachment);
  }

  protected closeAttachmentPreview(): void {
    this._previewAttachment.set(null);
  }

  protected toggleComposerSettings(): void {
    this._isComposerOpen.set(true);
    this._isComposerSettingsOpen.update(isOpen => !isOpen);
  }

  protected cancelComposer(): void {
    if (this._isPosting()) {
      return;
    }

    this._resetComposerDraft();
    this._isComposerOpen.set(false);
    this._isComposerSettingsOpen.set(false);
  }

  protected updateContent(value: string): void {
    this._isComposerOpen.set(true);
    this._newContent.set(value);
  }

  protected updateUseTranslations(value: boolean): void {
    this._isComposerOpen.set(true);
    this._isComposerSettingsOpen.set(true);
    this._useTranslations.set(value);
    if (value && !this._translationDraft().de.trim() && this._newContent().trim()) {
      this._translationDraft.update(draft => ({ ...draft, de: this._newContent() }));
    }
  }

  protected updateTranslation(language: 'de' | 'en' | 'fr', value: string): void {
    this._isComposerOpen.set(true);
    this._translationDraft.update(draft => ({ ...draft, [language]: value }));
  }

  protected onFilesSelected(files: FileList | null): void {
    this._isComposerOpen.set(true);
    this._isComposerSettingsOpen.set(true);
    const selected = [...this._selectedFiles(), ...Array.from(files ?? [])];
    const validationError = this._validateFiles(selected);
    if (validationError) {
      this._attachmentError.set(validationError);
      return;
    }

    this._selectedFiles.set(selected);
    this._attachmentError.set('');
  }

  protected removeSelectedFile(index: number): void {
    this._isComposerOpen.set(true);
    this._selectedFiles.update(files => files.filter((_, itemIndex) => itemIndex !== index));
    this._attachmentError.set(this._validateFiles(this._selectedFiles()) ?? '');
  }

  protected updateSelectedGroup(value: string): void {
    this._isComposerOpen.set(true);
    this._selectedGroupId.set(value);
    this._uiPreferences.setString(FEED_SELECTED_GROUP_KEY, value);
    this._allowComments.set(true);
  }

  protected updateAllowComments(value: boolean): void {
    this._isComposerOpen.set(true);
    this._isComposerSettingsOpen.set(true);
    this._allowComments.set(value);
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

  protected localizedPostContent(post: FeedPost): string {
    if (!post.translations) {
      return post.content;
    }

    const language = this._i18n.language();
    return post.translations[language] || post.translations.de || post.content;
  }

  protected formatFileSize(bytes: number): string {
    if (bytes < 1024 * 1024) {
      return `${Math.max(1, Math.round(bytes / 1024))} KB`;
    }

    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  protected attachmentAlt(fileName: string): string {
    return this._i18n.translate('feed.attachmentImageAlt', { fileName });
  }

  protected attachmentPreviewLabel(fileName: string): string {
    return this._i18n.translate('feed.openImagePreview', { fileName });
  }

  @HostListener('document:keydown.escape')
  protected closeAttachmentPreviewFromEscape(): void {
    this.closeAttachmentPreview();
  }

  protected canReactToPost(post: FeedPost): boolean {
    return post.group.canInteract;
  }

  protected groupTypeLabel(type: GroupType): string {
    switch (type) {
      case 'Course':
        return this._i18n.translate('common.course');
      case 'Official':
        return this._i18n.translate('groups.tab.official');
      case 'Campus':
        return this._i18n.translate('groups.tab.campus');
    }
  }

  protected commentPolicyLabel(group: CampusGroup): string {
    return group.settings.allowComments
      ? this._i18n.translate('groups.commentPolicy.open')
      : this._i18n.translate('groups.commentPolicy.closed');
  }

  protected groupName(group: CampusGroup): string {
    return this._i18n.groupName(group);
  }

  protected canPostToGroup(group: CampusGroup): boolean {
    return group.canPost;
  }

  protected navigateTo(route: string): void {
    void this._router.navigate([route]);
  }

  protected scheduleTime(event: TimetableEvent): string {
    if (event.isAllDay) {
      return this._i18n.translate('common.allDay');
    }

    return `${this._formatTime(event.start)}-${this._formatTime(event.end)}`;
  }

  protected scheduleDuration(event: TimetableEvent): string | null {
    if (event.isAllDay) {
      return null;
    }

    const durationMinutes = Math.max(1, Math.round((new Date(event.end).getTime() - new Date(event.start).getTime()) / 60000));
    if (durationMinutes < 60) {
      return `${durationMinutes} min`;
    }

    const hours = Math.floor(durationMinutes / 60);
    const minutes = durationMinutes % 60;
    const hourLabel = this._i18n.translate('common.hourShort');
    return minutes === 0 ? `${hours} ${hourLabel}` : `${hours} ${hourLabel} ${minutes} min`;
  }

  protected scheduleMeta(event: TimetableEvent): string | null {
    if (event.location) {
      return event.location;
    }

    if (!event.description) {
      return null;
    }

    const trimmed = event.description.replace(/\s+/g, ' ').trim();
    if (!trimmed) {
      return null;
    }

    return trimmed.length > 90 ? `${trimmed.slice(0, 90)}...` : trimmed;
  }

  protected initialsFor(value: string): string {
    return this._initialsFor(value);
  }

  private _loadTodaySchedule(): void {
    const course = this._resolveScheduleCourse();
    if (!course) {
      this._scheduleError.set(this._i18n.translate('timetable.chooseCourseState'));
      return;
    }

    const today = this._dateKey(new Date());
    this._scheduleDate.set(today);
    this._scheduleCourse.set(course);
    this._scheduleIsLoading.set(true);
    this._scheduleError.set('');

    this._timetableService.getTimetable(course, 14).subscribe({
      next: timetable => {
        const todaySchedule = timetable.days.find(day => day.date === today);
        this._scheduleCourse.set(timetable.course);
        this._scheduleTimezone.set(timetable.timezone);
        this._scheduleEvents.set([...(todaySchedule?.events ?? [])].sort((first, second) =>
          new Date(first.start).getTime() - new Date(second.start).getTime()
        ));
        this._scheduleIsLoading.set(false);
      },
      error: () => {
        this._scheduleEvents.set([]);
        this._scheduleError.set(this._i18n.translate('feed.dailyScheduleError'));
        this._scheduleIsLoading.set(false);
      },
    });
  }

  private _loadGroups(): void {
    this._groupsLoading.set(true);
    this._groupsError.set('');

    this._groupsService.getGroups().subscribe({
      next: groups => {
        this._groups.set(groups);
        this._selectDefaultGroup(groups);
        this._groupsLoading.set(false);
      },
      error: () => {
        this._groups.set([]);
        this._groupsError.set(this._i18n.translate('feed.groupsError'));
        this._groupsLoading.set(false);
      },
    });
  }

  private _selectDefaultGroup(groups: CampusGroup[]): void {
    const current = groups.find(group => group.id === this._selectedGroupId() && this.canPostToGroup(group));
    if (current) {
      this._uiPreferences.setString(FEED_SELECTED_GROUP_KEY, current.id);
      return;
    }

    const role = this._auth.userRole();
    const profileCourse = this._auth.userProfile()?.course.trim().toUpperCase();
    const courseGroup = groups.find(group =>
      group.type === 'Course' &&
      group.courseCode?.toUpperCase() === profileCourse &&
      this.canPostToGroup(group));
    const officialGroup = groups.find(group => group.type === 'Official' && this.canPostToGroup(group));
    const fallback = (role === 'Admin' ? officialGroup : courseGroup)
      ?? courseGroup
      ?? groups.find(group => group.type === 'Campus' && this.canPostToGroup(group))
      ?? groups.find(group => this.canPostToGroup(group));

    this._selectedGroupId.set(fallback?.id ?? '');
    this._uiPreferences.setString(FEED_SELECTED_GROUP_KEY, fallback?.id ?? '');
  }

  private _replacePost(updatedPost: FeedPost): void {
    this._posts.update(posts => posts.map(post => post.id === updatedPost.id ? updatedPost : post));
  }

  private _resetComposerDraft(): void {
    this._newContent.set('');
    this._translationDraft.set({ de: '', en: '', fr: '' });
    this._useTranslations.set(false);
    this._selectedFiles.set([]);
    this._attachmentError.set('');
    this._allowComments.set(true);
  }

  private _resolveScheduleCourse(): string {
    const profile = this._auth.userProfile();
    if (profile && profile.role !== 'Admin') {
      return this._timetableService.normalizeCourse(profile.course);
    }

    return this._timetableService.getStoredCourse();
  }

  private _initialsFor(value: string): string {
    const parts = value
      .replace(/@.*/, '')
      .split(/[.\s_-]+/)
      .filter(Boolean);

    if (parts.length === 0) {
      return 'CC';
    }

    return parts
      .slice(0, 2)
      .map(part => part[0].toUpperCase())
      .join('');
  }

  private _formatTime(value: string): string {
    return new Intl.DateTimeFormat(this._i18n.locale(), {
      hour: '2-digit',
      minute: '2-digit',
      timeZone: this._scheduleTimezone(),
    }).format(new Date(value));
  }

  private _dateKey(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private _formatDateLong(value: string): string {
    const [year, month, day] = value.split('-').map(Number);
    return new Intl.DateTimeFormat(this._i18n.locale(), { weekday: 'long', day: '2-digit', month: 'long' })
      .format(new Date(year, month - 1, day));
  }

  private _validateFiles(files: File[]): string | null {
    if (files.length > MAX_ATTACHMENT_COUNT) {
      return this._i18n.translate('feed.attachmentTooMany', { count: MAX_ATTACHMENT_COUNT });
    }

    for (const file of files) {
      if (file.size > MAX_ATTACHMENT_BYTES) {
        return this._i18n.translate('feed.attachmentTooLarge', { size: '10 MB' });
      }

      const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
      if (!ALLOWED_ATTACHMENT_EXTENSIONS.includes(extension)) {
        return this._i18n.translate('feed.attachmentTypeError');
      }
    }

    return null;
  }
}

