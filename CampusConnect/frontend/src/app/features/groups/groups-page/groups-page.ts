import { Component, ChangeDetectionStrategy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CampusGroup, GroupType } from '../../../core/models/group.model';
import { Groups } from '../../../core/services/groups';

type GroupTab = 'All' | GroupType;
type GroupPolicyFilter = 'All' | 'StudentPosts' | 'UniversityPosts' | 'Approval' | 'CommentsOpen' | 'CommentsClosed';

interface GroupTabItem {
  id: GroupTab;
  label: string;
  count: number;
}

@Component({
  selector: 'app-groups-page',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './groups-page.html',
  styleUrl: './groups-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupsPage implements OnInit {
  private readonly _groupsService = inject(Groups);
  private readonly _router = inject(Router);

  protected readonly _groups = signal<CampusGroup[]>([]);
  protected readonly _isLoading = signal(false);
  protected readonly _isCreating = signal(false);
  protected readonly _error = signal('');
  protected readonly _success = signal('');
  protected readonly _activeTab = signal<GroupTab>('All');
  protected readonly _searchQuery = signal('');
  protected readonly _policyFilter = signal<GroupPolicyFilter>('All');
  protected readonly _createName = signal('');
  protected readonly _createDescription = signal('');
  protected readonly _createAudience = signal('');
  protected readonly _officialGroups = computed(() => this._groups().filter(group => group.type === 'Official'));
  protected readonly _courseGroups = computed(() => this._groups().filter(group => group.type === 'Course'));
  protected readonly _socialGroups = computed(() => this._groups().filter(group => group.type === 'Social'));
  protected readonly _directoryFilteredGroups = computed(() => this._groups().filter(group => this._matchesSearch(group) && this._matchesPolicy(group)));
  protected readonly _tabs = computed<GroupTabItem[]>(() => [
    { id: 'All', label: 'Alle', count: this._directoryFilteredGroups().length },
    { id: 'Official', label: 'Offiziell', count: this._directoryFilteredGroups().filter(group => group.type === 'Official').length },
    { id: 'Course', label: 'Kurse', count: this._directoryFilteredGroups().filter(group => group.type === 'Course').length },
    { id: 'Social', label: 'Campus', count: this._directoryFilteredGroups().filter(group => group.type === 'Social').length },
  ]);
  protected readonly _filteredGroups = computed(() => {
    const activeTab = this._activeTab();
    return activeTab === 'All'
      ? this._directoryFilteredGroups()
      : this._directoryFilteredGroups().filter(group => group.type === activeTab);
  });
  protected readonly _canCreate = computed(() =>
    this._createName().trim().length > 0 &&
    this._createDescription().trim().length > 0 &&
    this._createAudience().trim().length > 0 &&
    !this._isCreating()
  );

  ngOnInit(): void {
    this._loadGroups();
  }

  protected setActiveTab(tab: GroupTab): void {
    this._activeTab.set(tab);
  }

  protected updateSearchQuery(value: string): void {
    this._searchQuery.set(value);
  }

  protected updatePolicyFilter(value: GroupPolicyFilter): void {
    this._policyFilter.set(value);
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

  protected openSettings(group: CampusGroup): void {
    if (!group.canManage) {
      return;
    }

    void this._router.navigate(['/groups', group.id, 'settings']);
  }

  protected createGroup(): void {
    if (!this._canCreate()) {
      return;
    }

    this._isCreating.set(true);
    this._error.set('');
    this._success.set('');
    this._groupsService.createGroup({
      name: this._createName().trim(),
      description: this._createDescription().trim(),
      audience: this._createAudience().trim(),
    }).subscribe({
      next: group => {
        this._groups.update(groups => [group, ...groups]);
        this._activeTab.set('Social');
        this._createName.set('');
        this._createDescription.set('');
        this._createAudience.set('');
        this._success.set('Gruppe wurde erstellt.');
        this._isCreating.set(false);
      },
      error: () => {
        this._error.set('Gruppe konnte nicht erstellt werden.');
        this._isCreating.set(false);
      },
    });
  }

  private _loadGroups(): void {
    this._isLoading.set(true);
    this._error.set('');

    this._groupsService.getGroups().subscribe({
      next: groups => {
        this._groups.set(groups);
        this._isLoading.set(false);
      },
      error: () => {
        this._groups.set([]);
        this._error.set('Gruppen konnten nicht geladen werden.');
        this._isLoading.set(false);
      },
    });
  }

  private _matchesSearch(group: CampusGroup): boolean {
    const query = this._searchQuery().trim().toLowerCase();
    if (!query) {
      return true;
    }

    return [group.name, group.description, group.audience, group.courseCode ?? '', group.ownerLabel]
      .some(value => value.toLowerCase().includes(query));
  }

  private _matchesPolicy(group: CampusGroup): boolean {
    switch (this._policyFilter()) {
      case 'StudentPosts':
        return group.settings.allowStudentPosts;
      case 'UniversityPosts':
        return !group.settings.allowStudentPosts;
      case 'Approval':
        return group.settings.requiresApproval;
      case 'CommentsOpen':
        return group.settings.allowComments;
      case 'CommentsClosed':
        return !group.settings.allowComments;
      case 'All':
        return true;
    }
  }
}
