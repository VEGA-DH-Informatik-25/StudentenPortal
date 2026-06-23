import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { I18n } from '../../../core/i18n/i18n';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { TranslationKey } from '../../../core/i18n/translations';
import { CampusGroup } from '../../../core/models/group.model';
import { Auth } from '../../../core/services/auth';
import { Groups } from '../../../core/services/groups';
import { GuidedTour } from '../../../core/services/guided-tour';

type OnboardingStep = 'welcome' | 'password' | 'loading' | 'tour' | 'groups';
type TourStep = { title: TranslationKey; body: TranslationKey };

@Component({
  selector: 'app-onboarding-page',
  standalone: true,
  imports: [FormsModule, TranslatePipe],
  templateUrl: './onboarding-page.html',
  styleUrl: './onboarding-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingPage implements OnInit {
  private readonly _auth = inject(Auth);
  private readonly _groupsService = inject(Groups);
  private readonly _router = inject(Router);
  private readonly _guidedTour = inject(GuidedTour);
  protected readonly _i18n = inject(I18n);

  protected readonly _step = signal<OnboardingStep>('welcome');
  protected readonly _tourIndex = signal(0);
  protected readonly _isLoading = signal(false);
  protected readonly _isSaving = signal(false);
  protected readonly _error = signal('');
  protected readonly _groups = signal<CampusGroup[]>([]);
  protected readonly _joinedGroupIds = signal<Set<string>>(new Set());
  protected readonly _tourSteps: TourStep[] = [
    { title: 'onboarding.tourNews', body: 'onboarding.tourNewsBody' },
    { title: 'onboarding.tourMensa', body: 'onboarding.tourMensaBody' },
    { title: 'onboarding.tourTimetable', body: 'onboarding.tourTimetableBody' },
    { title: 'onboarding.tourGrades', body: 'onboarding.tourGradesBody' },
    { title: 'onboarding.tourGroups', body: 'onboarding.tourGroupsBody' },
  ];
  protected readonly _suggestedGroups = computed(() => this._groups().filter(group => group.canJoin || group.canRequestJoin).slice(0, 3));
  protected readonly _passwordForm = { currentPassword: '', newPassword: '', confirmPassword: '' };

  ngOnInit(): void {
    const profile = this._auth.userProfile();
    if (profile?.mustChangePassword) {
      this._step.set('welcome');
    }
  }

  protected continueFromWelcome(): void {
    this._step.set('password');
  }

  protected passwordMeetsRequirements(): boolean {
    const password = this._passwordForm.newPassword;
    return password.length >= 8 && /[A-Z]/.test(password) && /[a-z]/.test(password) && /\d/.test(password) && /[^A-Za-z0-9]/.test(password);
  }

  protected changePassword(): void {
    if (!this.passwordMeetsRequirements() || this._passwordForm.newPassword !== this._passwordForm.confirmPassword) {
      this._error.set(this._i18n.translate('onboarding.passwordInvalid'));
      return;
    }

    this._isSaving.set(true);
    this._error.set('');
    this._auth.changeInitialPassword({ currentPassword: this._passwordForm.currentPassword, newPassword: this._passwordForm.newPassword }).subscribe({
      next: () => this.loadCampusData(),
      error: error => {
        this._error.set(error.error?.error ?? this._i18n.translate('onboarding.passwordError'));
        this._isSaving.set(false);
      },
    });
  }

  protected nextTourStep(): void {
    if (this._tourIndex() === this._tourSteps.length - 1) {
      this._step.set('groups');
      return;
    }
    this._tourIndex.update(index => index + 1);
  }

  protected skipTour(): void {
    this._step.set('groups');
  }

  protected joinGroup(group: CampusGroup): void {
    this._groupsService.joinGroup(group.id).subscribe({
      next: () => this._joinedGroupIds.update(ids => new Set([...ids, group.id])),
      error: () => this._error.set(this._i18n.translate('onboarding.groupError')),
    });
  }

  protected finish(): void {
    this._isSaving.set(true);
    this._auth.completeOnboarding().subscribe({
      next: () => this._router.navigate(['/feed']),
      error: error => {
        this._error.set(error.error?.error ?? this._i18n.translate('onboarding.finishError'));
        this._isSaving.set(false);
      },
    });
  }

  private loadCampusData(): void {
    this._step.set('loading');
    this._isLoading.set(true);
    forkJoin({ groups: this._groupsService.getGroups().pipe(catchError(() => of([]))) })
      .pipe(finalize(() => this._isLoading.set(false)))
      .subscribe(({ groups }) => {
        this._groups.set(groups);
        this._isSaving.set(false);
        this._guidedTour.start();
        this._router.navigate(['/feed']);
      });
  }
}
