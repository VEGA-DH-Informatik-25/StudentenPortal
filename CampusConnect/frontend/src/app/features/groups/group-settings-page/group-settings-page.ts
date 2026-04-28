import { Component, ChangeDetectionStrategy, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { GroupAccount, GroupSettings, GroupSettingsDetails } from '../../../core/models/group.model';
import { Groups } from '../../../core/services/groups';

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
  protected readonly _group = computed(() => this._details()?.group ?? null);
  protected readonly _accounts = computed(() => this._details()?.accounts ?? []);
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

  protected toggleAccount(account: GroupAccount, checked: boolean): void {
    if (this.isOwner(account)) {
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
    if (!group || !this._hasAssignmentChanges() || this._savingAssignments()) {
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
}
