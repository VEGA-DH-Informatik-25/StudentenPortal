import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Auth } from '../../../core/services/auth';
import { UserProfile } from '../../../core/models/auth.model';

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

  protected readonly _profile = signal<UserProfile | null>(null);
  protected readonly _isLoading = signal(false);
  protected readonly _isSaving = signal(false);
  protected readonly _error = signal('');
  protected readonly _success = signal('');

  protected readonly _form = {
    displayName: '',
    studyProgram: '',
    semester: 1,
    course: '',
  };

  ngOnInit(): void {
    const cachedProfile = this._auth.userProfile();
    if (cachedProfile) {
      this._setProfile(cachedProfile);
    }

    this._loadProfile();
  }

  protected onSave(): void {
    this._isSaving.set(true);
    this._error.set('');
    this._success.set('');

    this._auth.updateProfile({
      displayName: this._form.displayName.trim(),
      studyProgram: this._form.studyProgram.trim(),
      semester: Number(this._form.semester),
      course: this._form.course.trim(),
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
    this._form.studyProgram = profile.studyProgram;
    this._form.semester = profile.semester;
    this._form.course = profile.course;
  }

  private _readError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as { error?: string } | null;
      return body?.error ?? fallback;
    }

    return fallback;
  }
}
