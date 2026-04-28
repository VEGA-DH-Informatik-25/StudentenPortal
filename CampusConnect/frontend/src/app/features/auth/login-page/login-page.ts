import { HttpErrorResponse } from '@angular/common/http';
import { Component, ChangeDetectionStrategy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Course } from '../../../core/models/course.model';
import { Auth } from '../../../core/services/auth';
import { Courses } from '../../../core/services/courses';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage implements OnInit {
  private readonly _auth = inject(Auth);
  private readonly _coursesService = inject(Courses);
  private readonly _router = inject(Router);

  protected readonly _mode = signal<'login' | 'register'>('login');
  protected readonly _error = signal('');
  protected readonly _isLoading = signal(false);
  protected readonly _courses = signal<Course[]>([]);
  protected readonly _coursesLoading = signal(false);

  protected readonly _loginForm = { email: '', password: '' };
  protected readonly _registerForm = {
    email: '',
    password: '',
    displayName: '',
    course: '',
  };

  ngOnInit(): void {
    this._loadCourses();
  }

  protected switchMode(mode: 'login' | 'register'): void {
    this._mode.set(mode);
    this._error.set('');
  }

  protected onLogin(): void {
    this._isLoading.set(true);
    this._error.set('');
    this._auth.login({ email: this._loginForm.email, password: this._loginForm.password }).subscribe({
      next: () => this._router.navigate(['/feed']),
      error: err => {
        this._error.set(err.error?.error ?? 'Anmeldung fehlgeschlagen.');
        this._isLoading.set(false);
      },
    });
  }

  protected onRegister(): void {
    if (!this._registerForm.course) {
      this._error.set('Bitte wähle einen Kurs aus.');
      return;
    }

    this._isLoading.set(true);
    this._error.set('');
    this._auth.register(this._registerForm).subscribe({
      next: () => this._router.navigate(['/feed']),
      error: err => {
        this._error.set(err.error?.error ?? 'Registrierung fehlgeschlagen.');
        this._isLoading.set(false);
      },
    });
  }

  protected courseLabel(course: Course): string {
    return `${course.code} · ${course.studyProgram} · ${course.semester}. Semester`;
  }

  private _loadCourses(): void {
    this._coursesLoading.set(true);
    this._coursesService.getCourses().subscribe({
      next: courses => {
        this._courses.set(courses);
        if (!this._registerForm.course && courses.length > 0) {
          this._registerForm.course = courses[0].code;
        }
        this._coursesLoading.set(false);
      },
      error: error => {
        this._error.set(this._readError(error, 'Kurse konnten nicht geladen werden.'));
        this._coursesLoading.set(false);
      },
    });
  }

  private _readError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as { error?: string } | null;
      return body?.error ?? fallback;
    }

    return fallback;
  }
}
