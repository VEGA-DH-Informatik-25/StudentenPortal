import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, ChangeDetectionStrategy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminCourse, AdminUser } from '../../../core/models/admin.model';
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
  protected readonly _courses = signal<AdminCourse[]>([]);
  protected readonly _isLoading = signal(false);
  protected readonly _coursesLoading = signal(false);
  protected readonly _isCreatingCourse = signal(false);
  protected readonly _busyUserId = signal<string | null>(null);
  protected readonly _error = signal<string | null>(null);
  protected readonly _success = signal<string | null>(null);
  protected readonly _roles = ['Student', 'Lecturer', 'Admin'];
  protected readonly _courseForm = { code: '', studyProgram: '', semester: 1 };

  ngOnInit(): void {
    this.loadData();
  }

  protected loadData(): void {
    this.loadUsers();
    this.loadCourses();
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

  protected loadCourses(): void {
    this._coursesLoading.set(true);
    this._adminService.getCourses().subscribe({
      next: courses => {
        this._courses.set(courses);
        this._coursesLoading.set(false);
      },
      error: error => {
        this._error.set(this._readError(error));
        this._coursesLoading.set(false);
      },
    });
  }

  protected createCourse(): void {
    const code = this._courseForm.code.trim();
    const studyProgram = this._courseForm.studyProgram.trim();
    const semester = Number(this._courseForm.semester);

    if (!code || !studyProgram) {
      this._error.set('Bitte fülle alle Kursfelder aus.');
      return;
    }

    if (!Number.isInteger(semester) || semester < 1 || semester > 6) {
      this._error.set('Das Semester muss zwischen 1 und 6 liegen.');
      return;
    }

    this._isCreatingCourse.set(true);
    this._error.set(null);
    this._success.set(null);
    this._adminService.createCourse({ code, studyProgram, semester }).subscribe({
      next: course => {
        this._courses.update(courses => [...courses.filter(item => item.code !== course.code), course].sort((a, b) => a.code.localeCompare(b.code)));
        this._courseForm.code = '';
        this._courseForm.studyProgram = '';
        this._courseForm.semester = 1;
        this._success.set(`Kurs ${course.code} wurde angelegt.`);
        this._isCreatingCourse.set(false);
      },
      error: error => {
        this._error.set(this._readError(error));
        this._isCreatingCourse.set(false);
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

  protected updateCourse(user: AdminUser, courseCode: string): void {
    if (user.course === courseCode) {
      return;
    }

    this._busyUserId.set(user.id);
    this._error.set(null);
    this._success.set(null);
    this._adminService.updateUserCourse(user.id, courseCode).subscribe({
      next: updatedUser => {
        this._users.update(users => users.map(item => item.id === updatedUser.id ? updatedUser : item));
        this._success.set(`Kurs für ${updatedUser.displayName} wurde aktualisiert.`);
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

  protected courseLabel(course: AdminCourse): string {
    return `${course.code} · ${course.studyProgram} · ${course.semester}. Semester`;
  }

  protected courseExists(courseCode: string): boolean {
    return this._courses().some(course => course.code === courseCode);
  }

  private _readError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as { error?: string } | null;
      return body?.error ?? 'Admin-Daten konnten nicht geladen werden.';
    }

    return 'Admin-Daten konnten nicht geladen werden.';
  }
}
