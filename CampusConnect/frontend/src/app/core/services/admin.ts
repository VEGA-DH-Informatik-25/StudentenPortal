import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdminUser, UpdateUserRoleRequest } from '../models/admin.model';

@Injectable({ providedIn: 'root' })
export class Admin {
  private readonly _http = inject(HttpClient);

  getUsers(): Observable<AdminUser[]> {
    return this._http.get<AdminUser[]>('/api/admin/users');
  }

  updateUserRole(userId: string, role: string): Observable<AdminUser> {
    const request: UpdateUserRoleRequest = { role };
    return this._http.patch<AdminUser>(`/api/admin/users/${userId}/role`, request);
  }

  deleteUser(userId: string): Observable<void> {
    return this._http.delete<void>(`/api/admin/users/${userId}`);
  }
}
