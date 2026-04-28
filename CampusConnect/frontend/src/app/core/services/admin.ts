import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdminCreateCourseRequest, AdminCourse, AdminUser, UpdateUserCourseRequest, UpdateUserRoleRequest } from '../models/admin.model';

@Injectable({ providedIn: 'root' })
export class Admin {
  private readonly _http = inject(HttpClient);

  getUsers(): Observable<AdminUser[]> {
    return this._http.get<AdminUser[]>('/api/admin/users');
  }

  getCourses(): Observable<AdminCourse[]> {
    return this._http.get<AdminCourse[]>('/api/admin/courses');
  }

  createCourse(request: AdminCreateCourseRequest): Observable<AdminCourse> {
    return this._http.post<AdminCourse>('/api/admin/courses', request);
  }

  updateUserRole(userId: string, role: string): Observable<AdminUser> {
    const request: UpdateUserRoleRequest = { role };
    return this._http.patch<AdminUser>(`/api/admin/users/${userId}/role`, request);
  }

  updateUserCourse(userId: string, courseCode: string): Observable<AdminUser> {
    const request: UpdateUserCourseRequest = { courseCode };
    return this._http.patch<AdminUser>(`/api/admin/users/${userId}/course`, request);
  }

  deleteUser(userId: string): Observable<void> {
    return this._http.delete<void>(`/api/admin/users/${userId}`);
  }
}
