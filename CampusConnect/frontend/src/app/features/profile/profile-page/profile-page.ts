import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { I18n } from '../../../core/i18n/i18n';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Auth } from '../../../core/services/auth';
import { UserProfile } from '../../../core/models/auth.model';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [FormsModule, TranslatePipe],
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage implements OnInit {
  private readonly _auth = inject(Auth);
  private readonly _i18n = inject(I18n);

  protected readonly _profile = signal<UserProfile | null>(null);
  protected readonly _isLoading = signal(false);
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
        this._success.set(this._i18n.translate('profile.saved'));
        this._isSaving.set(false);
      },
      error: error => {
        this._error.set(this._i18n.readError(error, 'profile.saveError'));
        this._isSaving.set(false);
      },
    });
  }

  protected roleLabel(role: string): string {
    return this._i18n.roleLabel(role);
  }

  protected isNewHere(profile: UserProfile): boolean {
    return Date.now() - new Date(profile.createdAt).getTime() < 14 * 24 * 60 * 60 * 1000;
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
        this._error.set(this._i18n.readError(error, 'profile.loadError'));
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
}
