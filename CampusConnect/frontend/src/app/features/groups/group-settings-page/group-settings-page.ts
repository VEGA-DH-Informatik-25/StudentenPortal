import { Component, ChangeDetectionStrategy, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { GroupAccount, GroupSettings, GroupSettingsDetails } from '../../../core/models/group.model';
import { Groups } from '../../../core/services/groups';

type AccountFilter = 'All' | 'Assigned' | 'Unassigned' | 'Student' | 'Lecturer' | 'Admin';

@Component({
  selector: 'app-group-settings-page',
  standalone: true,
  imports: [],
  templateUrl: './group-settings-page.html',
  styleUrl: './group-settings-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupSettingsPage implements OnInit {
  private readonly _groupsService = inject(Groups);
  private readonly _route = inject(ActivatedRoute);
  private readonly _router = inject(Router);

  protected readonly _details = signal<GroupSettingsDetails | null>(null);
  protected readonly _selectedAccountIds = signal<string[]>([]);
  protected readonly _isLoading = signal(false);
  protected readonly _error = signal('');
  protected readonly _savingSetting = signal<keyof GroupSettings | ''>('');
  protected readonly _savingAssignments = signal(false);
  protected readonly _accountSearch = signal('');
  protected readonly _accountFilter = signal<AccountFilter>('All');
  protected readonly _group = computed(() => this._details()?.group ?? null);
  protected readonly _accounts = computed(() => this._details()?.accounts ?? []);
  protected readonly _filteredAccounts = computed(() => this._accounts().filter(account => this._matchesAccountSearch(account) && this._matchesAccountFilter(account)));
  protected readonly _selectedAccountCount = computed(() => this._selectedAccountIds().length);
  protected readonly _assignmentsLocked = computed(() => this._group()?.type === 'Course');
  protected readonly _hasAssignmentChanges = computed(() => {
    const originalIds = this._accounts()
      .filter(account => account.isAssigned)
      .map(account => account.id)
      .sort()
      .join('|');
    const selectedIds = [...this._selectedAccountIds()].sort().join('|');
    return originalIds !== selectedIds;
  });

  ngOnInit(): void {
    const groupId = this._route.snapshot.paramMap.get('id');
    if (!groupId) {
      this._error.set('Gruppe wurde nicht gefunden.');
      return;
    }

    this._loadDetails(groupId);
  }

  protected backToGroups(): void {
    void this._router.navigate(['/groups']);
  }

  protected updateSetting(setting: keyof GroupSettings, checked: boolean): void {
    const group = this._group();
    if (!group || this._savingSetting()) {
      return;
    }

    this._savingSetting.set(setting);
    this._error.set('');
    this._groupsService.updateSettings(group.id, { ...group.settings, [setting]: checked }).subscribe({
      next: updatedGroup => {
        this._details.update(details => details ? { ...details, group: updatedGroup } : details);
        this._savingSetting.set('');
      },
      error: () => {
        this._error.set('Einstellung konnte nicht gespeichert werden.');
        this._savingSetting.set('');
      },
    });
  }

  protected isSettingBusy(setting: keyof GroupSettings): boolean {
    return this._savingSetting() === setting;
  }

  protected isAccountSelected(account: GroupAccount): boolean {
    return this._selectedAccountIds().includes(account.id);
  }

  protected isOwner(account: GroupAccount): boolean {
    return this._group()?.ownerUserId === account.id;
  }

  protected updateAccountSearch(value: string): void {
    this._accountSearch.set(value);
  }

  protected updateAccountFilter(value: AccountFilter): void {
    this._accountFilter.set(value);
  }

  protected toggleAccount(account: GroupAccount, checked: boolean): void {
    if (this.isOwner(account) || this._assignmentsLocked()) {
      return;
    }

    this._selectedAccountIds.update(ids => {
      const selected = new Set(ids);
      if (checked) {
        selected.add(account.id);
      } else {
        selected.delete(account.id);
      }

      return [...selected];
    });
  }

  protected saveAssignments(): void {
    const group = this._group();
    if (!group || this._assignmentsLocked() || !this._hasAssignmentChanges() || this._savingAssignments()) {
      return;
    }

    this._savingAssignments.set(true);
    this._error.set('');
    this._groupsService.updateAssignments(group.id, { userIds: this._selectedAccountIds() }).subscribe({
      next: details => {
        this._setDetails(details);
        this._savingAssignments.set(false);
      },
      error: () => {
        this._error.set('Konten konnten nicht zugewiesen werden.');
        this._savingAssignments.set(false);
      },
    });
  }

  private _loadDetails(groupId: string): void {
    this._isLoading.set(true);
    this._error.set('');

    this._groupsService.getSettings(groupId).subscribe({
      next: details => {
        this._setDetails(details);
        this._isLoading.set(false);
      },
      error: () => {
        this._details.set(null);
        this._error.set('Du kannst diese Gruppeneinstellungen nicht bearbeiten.');
        this._isLoading.set(false);
      },
    });
  }

  private _setDetails(details: GroupSettingsDetails): void {
    this._details.set(details);
    this._selectedAccountIds.set(details.accounts.filter(account => account.isAssigned).map(account => account.id));
  }

  private _matchesAccountSearch(account: GroupAccount): boolean {
    const query = this._accountSearch().trim().toLowerCase();
    if (!query) {
      return true;
    }

    return [account.displayName, account.email, account.role, account.course || '']
      .some(value => value.toLowerCase().includes(query));
  }

  private _matchesAccountFilter(account: GroupAccount): boolean {
    switch (this._accountFilter()) {
      case 'Assigned':
        return this.isAccountSelected(account);
      case 'Unassigned':
        return !this.isAccountSelected(account);
      case 'Student':
      case 'Lecturer':
      case 'Admin':
        return account.role === this._accountFilter();
      case 'All':
        return true;
    }
  }
}
