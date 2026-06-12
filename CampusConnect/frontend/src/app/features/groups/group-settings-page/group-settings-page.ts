import { DatePipe } from '@angular/common';
import { Component, ChangeDetectionStrategy, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { I18n } from '../../../core/i18n/i18n';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Course } from '../../../core/models/course.model';
import { FeedPost } from '../../../core/models/feed.model';
import { CampusGroup, GroupCandidate, GroupJoinRule, GroupMember, GroupRequest, GroupRole, GroupSettings, GroupSettingsDetails } from '../../../core/models/group.model';
import { Courses } from '../../../core/services/courses';
import { Feed } from '../../../core/services/feed';
import { Groups } from '../../../core/services/groups';

@Component({
  selector: 'app-group-settings-page',
  standalone: true,
  imports: [DatePipe, TranslatePipe],
  templateUrl: './group-settings-page.html',
  styleUrl: './group-settings-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupSettingsPage implements OnInit {
  private readonly _groupsService = inject(Groups);
  private readonly _coursesService = inject(Courses);
  private readonly _feedService = inject(Feed);
  protected readonly _i18n = inject(I18n);
  private readonly _route = inject(ActivatedRoute);
  private readonly _router = inject(Router);

  protected readonly _details = signal<GroupSettingsDetails | null>(null);
  protected readonly _isLoading = signal(false);
  protected readonly _error = signal('');
  protected readonly _savingSetting = signal<keyof GroupSettings | ''>('');

  protected readonly _searchTerm = signal('');
  protected readonly _candidates = signal<GroupCandidate[]>([]);
  protected readonly _searching = signal(false);
  protected readonly _hasSearched = signal(false);
  protected readonly _busyUserId = signal('');

  protected readonly _courses = signal<Course[]>([]);
  protected readonly _selectedCourseCode = signal('');
  protected readonly _addingCourse = signal(false);
  protected readonly _pendingPosts = signal<FeedPost[]>([]);
  protected readonly _busyPostId = signal('');
  protected readonly _deleteConfirmationOpen = signal(false);
  protected readonly _deletingGroup = signal(false);
  protected readonly _leaveConfirmationOpen = signal(false);
  protected readonly _leavingGroup = signal(false);
  protected readonly _selectedNewOwnerId = signal('');

  protected readonly _group = computed(() => this._details()?.group ?? null);
  protected readonly _members = computed(() => this._details()?.members ?? []);
  protected readonly _joinRequests = computed(() => this._details()?.joinRequests ?? []);
  protected readonly _invitations = computed(() => this._details()?.invitations ?? []);
  protected readonly _canEditSettings = computed(() => this._group()?.canEditSettings ?? false);
  protected readonly _canManageMembers = computed(() => this._group()?.canManageMembers ?? false);
  protected readonly _canAppointModerator = computed(() => this._group()?.canAppointModerator ?? false);
  protected readonly _isCourseManaged = computed(() => this._group()?.isCourseManaged ?? false);
  protected readonly _isSystemAdminAccess = computed(() => this._group()?.isSystemAdminAccess ?? false);
  protected readonly _canDelete = computed(() => this._group()?.canDelete ?? false);
  protected readonly _canLeaveGroup = computed(() => {
    const group = this._group();
    return !!group && group.isAssigned && !group.isCourseManaged && !group.isSystemAdminAccess;
  });
  protected readonly _leaveOwnerCandidates = computed(() => this._members().filter(member => !member.isOwner));
  protected readonly _requiresNewOwner = computed(() => this._group()?.groupRole === 'Owner' && this._leaveOwnerCandidates().length > 0);
  protected readonly _canConfirmLeave = computed(() => this._canLeaveGroup() && !this._leavingGroup() && (!this._requiresNewOwner() || !!this._selectedNewOwnerId()));

  ngOnInit(): void {
    const groupId = this._route.snapshot.paramMap.get('id');
    if (!groupId) {
      this._error.set(this._i18n.translate('groups.detailNotFound'));
      return;
    }

    this._loadDetails(groupId);
  }

  protected backToGroups(): void {
    void this._router.navigate(['/groups']);
  }

  protected updateSetting(setting: keyof GroupSettings, checked: boolean): void {
    const group = this._group();
    if (!group || !this._canEditSettings() || this._savingSetting()) {
      return;
    }

    this._savingSetting.set(setting);
    this._error.set('');
    this._groupsService.updateSettings(group.id, { ...group.settings, [setting]: checked }).subscribe({
      next: updatedGroup => {
        this._details.update(details => (details ? { ...details, group: updatedGroup } : details));
        this._savingSetting.set('');
      },
      error: () => {
        this._error.set(this._i18n.translate('groups.settingSaveError'));
        this._savingSetting.set('');
      },
    });
  }

  protected isSettingBusy(setting: keyof GroupSettings): boolean {
    return this._savingSetting() === setting;
  }

  protected updateJoinRule(value: string): void {
    const group = this._group();
    if (!group || !this._canEditSettings() || this._savingSetting()) {
      return;
    }

    const joinRule = value as GroupJoinRule;
    if (joinRule === group.settings.joinRule) {
      return;
    }

    this._savingSetting.set('joinRule');
    this._error.set('');
    this._groupsService.updateSettings(group.id, { ...group.settings, joinRule }).subscribe({
      next: updatedGroup => {
        this._details.update(details => (details ? { ...details, group: updatedGroup } : details));
        this._savingSetting.set('');
      },
      error: () => {
        this._error.set(this._i18n.translate('groups.settingSaveError'));
        this._savingSetting.set('');
      },
    });
  }

  protected joinRuleLabel(rule: string): string {
    switch (rule) {
      case 'RequestRequired':
        return this._i18n.translate('groups.joinRule.request');
      case 'InviteOnly':
        return this._i18n.translate('groups.joinRule.invite');
      default:
        return this._i18n.translate('groups.joinRule.open');
    }
  }

  protected approveRequest(request: GroupRequest): void {
    const group = this._group();
    if (!group || this._busyUserId()) {
      return;
    }

    this._busyUserId.set(request.id);
    this._error.set('');
    this._groupsService.approveRequest(group.id, request.id).subscribe({
      next: details => {
        this._setDetails(details);
        this._busyUserId.set('');
      },
      error: () => {
        this._error.set(this._i18n.translate('groups.requestActionError'));
        this._busyUserId.set('');
      },
    });
  }

  protected rejectRequest(request: GroupRequest): void {
    const group = this._group();
    if (!group || this._busyUserId()) {
      return;
    }

    this._busyUserId.set(request.id);
    this._error.set('');
    this._groupsService.rejectRequest(group.id, request.id).subscribe({
      next: details => {
        this._setDetails(details);
        this._busyUserId.set('');
      },
      error: () => {
        this._error.set(this._i18n.translate('groups.requestActionError'));
        this._busyUserId.set('');
      },
    });
  }

  protected inviteCandidate(candidate: GroupCandidate): void {
    const group = this._group();
    if (!group || this._busyUserId()) {
      return;
    }

    this._busyUserId.set(candidate.id);
    this._error.set('');
    this._groupsService.inviteMembers(group.id, { userIds: [candidate.id] }).subscribe({
      next: details => {
        this._setDetails(details);
        this._candidates.update(items => items.filter(item => item.id !== candidate.id));
        this._busyUserId.set('');
      },
      error: () => {
        this._error.set(this._i18n.translate('groups.inviteError'));
        this._busyUserId.set('');
      },
    });
  }

  protected cancelInvitation(invitation: GroupRequest): void {
    const group = this._group();
    if (!group || this._busyUserId()) {
      return;
    }

    this._busyUserId.set(invitation.id);
    this._error.set('');
    this._groupsService.cancelInvitation(group.id, invitation.id).subscribe({
      next: details => {
        this._setDetails(details);
        this._busyUserId.set('');
      },
      error: () => {
        this._error.set(this._i18n.translate('groups.cancelInvitationError'));
        this._busyUserId.set('');
      },
    });
  }

  protected approvePost(post: FeedPost): void {
    if (this._busyPostId()) {
      return;
    }

    this._busyPostId.set(post.id);
    this._error.set('');
    this._feedService.approvePost(post.id).subscribe({
      next: () => {
        this._pendingPosts.update(posts => posts.filter(item => item.id !== post.id));
        this._busyPostId.set('');
      },
      error: () => {
        this._error.set(this._i18n.translate('groups.moderationActionError'));
        this._busyPostId.set('');
      },
    });
  }

  protected rejectPost(post: FeedPost): void {
    if (this._busyPostId()) {
      return;
    }

    this._busyPostId.set(post.id);
    this._error.set('');
    this._feedService.deletePost(post.id).subscribe({
      next: () => {
        this._pendingPosts.update(posts => posts.filter(item => item.id !== post.id));
        this._busyPostId.set('');
      },
      error: () => {
        this._error.set(this._i18n.translate('groups.moderationActionError'));
        this._busyPostId.set('');
      },
    });
  }

  protected isPostBusy(post: FeedPost): boolean {
    return this._busyPostId() === post.id;
  }

  protected openDeleteConfirmation(): void {
    this._deleteConfirmationOpen.set(true);
  }

  protected closeDeleteConfirmation(): void {
    if (!this._deletingGroup()) {
      this._deleteConfirmationOpen.set(false);
    }
  }

  protected openLeaveConfirmation(): void {
    if (!this._canLeaveGroup()) {
      return;
    }

    this._selectedNewOwnerId.set(this._leaveOwnerCandidates()[0]?.id ?? '');
    this._leaveConfirmationOpen.set(true);
  }

  protected closeLeaveConfirmation(): void {
    if (!this._leavingGroup()) {
      this._leaveConfirmationOpen.set(false);
    }
  }

  protected updateSelectedNewOwner(userId: string): void {
    this._selectedNewOwnerId.set(userId);
  }

  protected leaveGroup(): void {
    const group = this._group();
    if (!group || !this._canConfirmLeave()) {
      return;
    }

    this._leavingGroup.set(true);
    this._error.set('');
    this._groupsService.leaveGroup(group.id, {
      newOwnerUserId: this._requiresNewOwner() ? this._selectedNewOwnerId() : null,
    }).subscribe({
      next: () => void this._router.navigate(['/groups']),
      error: () => {
        this._error.set(this._i18n.translate('groups.leaveGroupError'));
        this._leavingGroup.set(false);
      },
    });
  }

  protected deleteGroup(): void {
    const group = this._group();
    if (!group || !this._canDelete() || this._deletingGroup()) {
      return;
    }

    this._deletingGroup.set(true);
    this._error.set('');
    this._groupsService.deleteGroup(group.id).subscribe({
      next: () => void this._router.navigate(['/groups']),
      error: () => {
        this._error.set(this._i18n.translate('groups.deleteError'));
        this._deletingGroup.set(false);
      },
    });
  }

  protected isRequestBusy(request: GroupRequest): boolean {
    return this._busyUserId() === request.id;
  }

  protected updateSearchTerm(value: string): void {
    this._searchTerm.set(value);
  }

  protected searchCandidates(): void {
    const group = this._group();
    const query = this._searchTerm().trim();
    if (!group || query.length < 2) {
      this._candidates.set([]);
      this._hasSearched.set(false);
      return;
    }

    this._searching.set(true);
    this._error.set('');
    this._groupsService.searchCandidates(group.id, query).subscribe({
      next: candidates => {
        this._candidates.set(candidates);
        this._hasSearched.set(true);
        this._searching.set(false);
      },
      error: () => {
        this._error.set(this._i18n.translate('groups.candidateSearchError'));
        this._searching.set(false);
      },
    });
  }

  protected addMember(candidate: GroupCandidate): void {
    const group = this._group();
    if (!group || this._busyUserId()) {
      return;
    }

    this._busyUserId.set(candidate.id);
    this._error.set('');
    this._groupsService.addMembers(group.id, { userIds: [candidate.id] }).subscribe({
      next: details => {
        this._setDetails(details);
        this._candidates.update(items => items.filter(item => item.id !== candidate.id));
        this._busyUserId.set('');
      },
      error: () => {
        this._error.set(this._i18n.translate('groups.addMembersError'));
        this._busyUserId.set('');
      },
    });
  }

  protected updateSelectedCourse(value: string): void {
    this._selectedCourseCode.set(value);
  }

  protected addCourse(): void {
    const group = this._group();
    const courseCode = this._selectedCourseCode().trim();
    if (!group || !courseCode || this._addingCourse()) {
      return;
    }

    this._addingCourse.set(true);
    this._error.set('');
    this._groupsService.addCourse(group.id, { courseCode }).subscribe({
      next: details => {
        this._setDetails(details);
        this._selectedCourseCode.set('');
        this._addingCourse.set(false);
      },
      error: () => {
        this._error.set(this._i18n.translate('groups.addCourseError'));
        this._addingCourse.set(false);
      },
    });
  }

  protected removeMember(member: GroupMember): void {
    const group = this._group();
    if (!group || member.isOwner || this._busyUserId()) {
      return;
    }

    this._busyUserId.set(member.id);
    this._error.set('');
    this._groupsService.removeMember(group.id, member.id).subscribe({
      next: details => {
        this._setDetails(details);
        this._busyUserId.set('');
      },
      error: () => {
        this._error.set(this._i18n.translate('groups.removeMemberError'));
        this._busyUserId.set('');
      },
    });
  }

  protected changeMemberRole(member: GroupMember, role: string): void {
    const group = this._group();
    if (!group || member.isOwner || this._busyUserId()) {
      return;
    }

    const nextRole = role as GroupRole;
    if (nextRole === member.groupRole) {
      return;
    }

    if (nextRole === 'Moderator' && !this._canAppointModerator()) {
      this._error.set(this._i18n.translate('groups.role.ownerOnlyModerator'));
      return;
    }

    this._busyUserId.set(member.id);
    this._error.set('');
    this._groupsService.setMemberRole(group.id, member.id, { role: nextRole }).subscribe({
      next: details => {
        this._setDetails(details);
        this._busyUserId.set('');
      },
      error: () => {
        this._error.set(this._i18n.translate('groups.roleChangeError'));
        this._busyUserId.set('');
      },
    });
  }

  protected canEditMemberRole(member: GroupMember): boolean {
    return !member.isOwner && this._canManageMembers();
  }

  protected canAssignModeratorRole(member: GroupMember): boolean {
    return this._canAppointModerator() || member.groupRole === 'Moderator';
  }

  protected isMemberBusy(member: GroupMember): boolean {
    return this._busyUserId() === member.id;
  }

  protected isCandidateBusy(candidate: GroupCandidate): boolean {
    return this._busyUserId() === candidate.id;
  }

  protected roleLabel(role: string): string {
    return this._i18n.roleLabel(role);
  }

  protected groupRoleLabel(role: string): string {
    return this._i18n.groupRoleLabel(role);
  }

  protected groupName(group: CampusGroup): string {
    return this._i18n.groupName(group);
  }

  protected groupDescription(group: CampusGroup): string {
    return this._i18n.groupDescription(group);
  }

  protected groupAudience(group: CampusGroup): string {
    return this._i18n.groupAudience(group);
  }

  protected groupOwnerLabel(group: CampusGroup): string {
    return this._i18n.groupOwnerLabel(group);
  }

  private _loadDetails(groupId: string): void {
    this._isLoading.set(true);
    this._error.set('');

    this._groupsService.getSettings(groupId).subscribe({
      next: details => {
        this._setDetails(details);
        this._isLoading.set(false);
        this._loadPendingPosts(groupId);
        if (details.group.canManageMembers && !details.group.isCourseManaged) {
          this._loadCourses();
        }
      },
      error: () => {
        this._details.set(null);
        this._error.set(this._i18n.translate('groups.settingsError'));
        this._isLoading.set(false);
      },
    });
  }

  private _loadCourses(): void {
    this._coursesService.getCourses().subscribe({
      next: courses => this._courses.set(courses.filter(course => course.isActive)),
      error: () => this._courses.set([]),
    });
  }

  private _loadPendingPosts(groupId: string): void {
    this._feedService.getPendingPosts(groupId).subscribe({
      next: posts => this._pendingPosts.set(posts),
      error: () => this._pendingPosts.set([]),
    });
  }

  private _setDetails(details: GroupSettingsDetails): void {
    this._details.set(details);
  }
}
