import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Auth } from '../../../core/services/auth';
import { Course } from '../../../core/models/course.model';
import { UserProfile } from '../../../core/models/auth.model';
import { Courses } from '../../../core/services/courses';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage implements OnInit {
  private readonly _auth = inject(Auth);
  private readonly _coursesService = inject(Courses);

  protected readonly _profile = signal<UserProfile | null>(null);
  protected readonly _courses = signal<Course[]>([]);
  protected readonly _isLoading = signal(false);
  protected readonly _coursesLoading = signal(false);
  protected readonly _isSaving = signal(false);
  protected readonly _error = signal('');
  protected readonly _success = signal('');

  protected readonly _form = {
    displayName: '',
    course: '',
    phoneNumber: '',
    location: '',
    profileNote: '',
  };

  ngOnInit(): void {
    const cachedProfile = this._auth.userProfile();
    if (cachedProfile) {
      this._setProfile(cachedProfile);
    }

    this._loadCourses();
    this._loadProfile();
  }

  protected onSave(): void {
    this._isSaving.set(true);
    this._error.set('');
    this._success.set('');

    this._auth.updateProfile({
      displayName: this._form.displayName.trim(),
      course: this._form.course.trim(),
      phoneNumber: this._form.phoneNumber.trim(),
      location: this._form.location.trim(),
      profileNote: this._form.profileNote.trim(),
    }).subscribe({
      next: profile => {
        this._setProfile(profile);
        this._success.set('Profil wurde gespeichert.');
        this._isSaving.set(false);
      },
      error: error => {
        this._error.set(this._readError(error, 'Profil konnte nicht gespeichert werden.'));
        this._isSaving.set(false);
      },
    });
  }

  protected selectedCourse(): Course | null {
    return this._courses().find(course => course.code === this._form.course) ?? null;
  }

  protected courseLabel(course: Course): string {
    return `${course.code} · ${course.studyProgram} · ${course.semester}. Semester`;
  }

  private _loadCourses(): void {
    this._coursesLoading.set(true);
    this._coursesService.getCourses().subscribe({
      next: courses => {
        this._courses.set(courses);
        this._coursesLoading.set(false);
      },
      error: error => {
        this._error.set(this._readError(error, 'Kurse konnten nicht geladen werden.'));
        this._coursesLoading.set(false);
      },
    });
  }

  private _loadProfile(): void {
    this._isLoading.set(true);
    this._error.set('');

    this._auth.loadProfile().subscribe({
      next: profile => {
        this._setProfile(profile);
        this._isLoading.set(false);
      },
      error: error => {
        this._error.set(this._readError(error, 'Profil konnte nicht geladen werden.'));
        this._isLoading.set(false);
      },
    });
  }

  private _setProfile(profile: UserProfile): void {
    this._profile.set(profile);
    this._form.displayName = profile.displayName;
    this._form.course = profile.course;
    this._form.phoneNumber = profile.phoneNumber;
    this._form.location = profile.location;
    this._form.profileNote = profile.profileNote;
  }

  private _readError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as { error?: string } | null;
      return body?.error ?? fallback;
    }

    return fallback;
  }
}
