import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslationKey } from '../i18n/translations';
import { Auth } from './auth';
import { UiPreferences } from './ui-preferences';

export interface GuidedTourStep {
  selector: string;
  title: TranslationKey;
  body: TranslationKey;
  route?: string;
  clickRequired?: boolean;
  inputRequired?: boolean;
  final?: boolean;
}

type GuidedTourKind = 'onboarding' | 'groups';

@Injectable({ providedIn: 'root' })
export class GuidedTour {
  private readonly _router = inject(Router);
  private readonly _auth = inject(Auth);
  private readonly _uiPreferences = inject(UiPreferences);
  private readonly _kind = signal<GuidedTourKind>('onboarding');
  readonly active = signal(false);
  readonly stepIndex = signal(0);
  readonly inputCompleted = signal(false);
  private readonly _onboardingSteps: GuidedTourStep[] = [
    { selector: '[data-tour="feed"]', title: 'onboarding.tourNews', body: 'onboarding.tourNavNews' },
    { selector: '[data-tour="mensa"]', title: 'onboarding.tourMensa', body: 'onboarding.tourNavMensa' },
    { selector: '[data-tour="timetable"]', title: 'onboarding.tourTimetable', body: 'onboarding.tourNavTimetable' },
    { selector: '[data-tour="calendar"]', title: 'onboarding.tourCalendar', body: 'onboarding.tourNavCalendar' },
    { selector: '[data-tour="grades"]', title: 'onboarding.tourGrades', body: 'onboarding.tourNavGrades' },
    { selector: '[data-tour="groups"]', title: 'onboarding.tourGroups', body: 'onboarding.tourNavGroups' },
    { selector: '[data-tour="contacts"]', title: 'onboarding.tourContacts', body: 'onboarding.tourNavContacts' },
    { selector: '[data-tour="quick-access"]', title: 'onboarding.tourLinks', body: 'onboarding.tourQuickAccess', final: true },
  ];
  private readonly _groupsSteps: GuidedTourStep[] = [
    { selector: '[data-tour="groups-types"]', title: 'onboarding.tourGroups', body: 'onboarding.tourGroupsTypes' },
    { selector: '[data-tour="groups-discover"]', title: 'onboarding.tourGroups', body: 'onboarding.tourDiscoverClick', clickRequired: true },
    { selector: '[data-tour="groups-discover"]', title: 'onboarding.tourGroups', body: 'onboarding.tourGroupsBody', final: true },
  ];

  get steps(): GuidedTourStep[] {
    return this._kind() === 'groups' ? this._groupsSteps : this._onboardingSteps;
  }

  start(): void {
    this._kind.set('onboarding');
    this.stepIndex.set(0);
    this.inputCompleted.set(false);
    this.active.set(true);
  }

  startGroupsTour(): void {
    const preferenceKey = this._groupsTourPreferenceKey();
    if (!preferenceKey || this._uiPreferences.getString(preferenceKey) !== 'pending' || this.active()) {
      return;
    }

    this._kind.set('groups');
    this.stepIndex.set(0);
    this.inputCompleted.set(false);
    this.active.set(true);
  }

  next(): void {
    const nextIndex = this.stepIndex() + 1;
    if (this.steps[this.stepIndex()]?.final || nextIndex >= this.steps.length) { this.finish(); return; }
    this.stepIndex.set(nextIndex);
    this.inputCompleted.set(false);
    const route = this.steps[nextIndex].route;
    if (route) { this._router.navigate([route]); }
  }
  finish(): void {
    this.active.set(false);
    if (this._kind() === 'groups') {
      const preferenceKey = this._groupsTourPreferenceKey();
      if (preferenceKey) {
        this._uiPreferences.setString(preferenceKey, '');
      }
      return;
    }

    this._auth.completeOnboarding().subscribe({
      next: () => {
        const preferenceKey = this._groupsTourPreferenceKey();
        if (preferenceKey) {
          this._uiPreferences.setString(preferenceKey, 'pending');
        }
        this._router.navigate(['/feed']);
      },
    });
  }
  completeInput(): void { this.inputCompleted.set(true); }

  private _groupsTourPreferenceKey(): string | null {
    const userId = this._auth.userProfile()?.id;
    return userId ? `campusconnect.onboarding.groupsTour.${userId}` : null;
  }
}
