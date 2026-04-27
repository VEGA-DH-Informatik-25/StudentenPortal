import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, ChangeDetectionStrategy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminUser } from '../../../core/models/admin.model';
import { Admin } from '../../../core/services/admin';

@Component({
  selector: 'app-admin-page',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './admin-page.html',
  styleUrl: './admin-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminPage implements OnInit {
  private readonly _adminService = inject(Admin);

  protected readonly _users = signal<AdminUser[]>([]);
  protected readonly _isLoading = signal(false);
  protected readonly _busyUserId = signal<string | null>(null);
  protected readonly _error = signal<string | null>(null);
  protected readonly _success = signal<string | null>(null);
  protected readonly _roles = ['Student', 'Lecturer', 'Admin'];

  ngOnInit(): void {
    this.loadUsers();
  }

  protected loadUsers(): void {
    this._isLoading.set(true);
    this._error.set(null);
    this._adminService.getUsers().subscribe({
      next: users => {
        this._users.set(users);
        this._isLoading.set(false);
      },
      error: error => {
        this._error.set(this._readError(error));
        this._isLoading.set(false);
      },
    });
  }

  protected updateRole(user: AdminUser, role: string): void {
    if (user.role === role) {
      return;
    }

    this._busyUserId.set(user.id);
    this._error.set(null);
    this._success.set(null);
    this._adminService.updateUserRole(user.id, role).subscribe({
      next: updatedUser => {
        this._users.update(users => users.map(item => item.id === updatedUser.id ? updatedUser : item));
        this._success.set(`Rolle für ${updatedUser.displayName} wurde aktualisiert.`);
        this._busyUserId.set(null);
      },
      error: error => {
        this._error.set(this._readError(error));
        this._busyUserId.set(null);
      },
    });
  }

  protected deleteUser(user: AdminUser): void {
    if (!confirm(`${user.displayName} wirklich löschen?`)) {
      return;
    }

    this._busyUserId.set(user.id);
    this._error.set(null);
    this._success.set(null);
    this._adminService.deleteUser(user.id).subscribe({
      next: () => {
        this._users.update(users => users.filter(item => item.id !== user.id));
        this._success.set(`${user.displayName} wurde gelöscht.`);
        this._busyUserId.set(null);
      },
      error: error => {
        this._error.set(this._readError(error));
        this._busyUserId.set(null);
      },
    });
  }

  private _readError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as { error?: string } | null;
      return body?.error ?? 'Admin-Daten konnten nicht geladen werden.';
    }

    return 'Admin-Daten konnten nicht geladen werden.';
  }
}
