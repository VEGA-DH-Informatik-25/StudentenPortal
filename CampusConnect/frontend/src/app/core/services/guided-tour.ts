import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslationKey } from '../i18n/translations';
import { Auth } from './auth';

export interface GuidedTourStep {
  selector: string;
  title: TranslationKey;
  body: TranslationKey;
  route?: string;
  clickRequired?: boolean;
  inputRequired?: boolean;
  final?: boolean;
}

@Injectable({ providedIn: 'root' })
export class GuidedTour {
  private readonly _router = inject(Router);
  private readonly _auth = inject(Auth);
  readonly active = signal(false);
  readonly stepIndex = signal(0);
  readonly inputCompleted = signal(false);
  readonly steps: GuidedTourStep[] = [
    { selector: '[data-tour="feed"]', title: 'onboarding.tourNews', body: 'onboarding.tourNavNews' },
    { selector: '[data-tour="mensa"]', title: 'onboarding.tourMensa', body: 'onboarding.tourNavMensa' },
    { selector: '[data-tour="timetable"]', title: 'onboarding.tourTimetable', body: 'onboarding.tourNavTimetable' },
    { selector: '[data-tour="calendar"]', title: 'onboarding.tourCalendar', body: 'onboarding.tourNavCalendar' },
    { selector: '[data-tour="grades"]', title: 'onboarding.tourGrades', body: 'onboarding.tourNavGrades' },
    { selector: '[data-tour="groups"]', title: 'onboarding.tourGroups', body: 'onboarding.tourNavGroups' },
    { selector: '[data-tour="contacts"]', title: 'onboarding.tourContacts', body: 'onboarding.tourNavContacts' },
    { selector: '[data-tour="quick-access"]', title: 'onboarding.tourNews', body: 'onboarding.tourQuickAccess' },
    { selector: '[data-tour="mensa"]', title: 'onboarding.tourMensa', body: 'onboarding.tourClickMensa', clickRequired: true },
    { selector: '[data-tour="mensa-page"]', title: 'onboarding.tourMensa', body: 'onboarding.tourMensaBody' },
    { selector: '[data-tour="timetable"]', title: 'onboarding.tourTimetable', body: 'onboarding.tourClickTimetable', clickRequired: true },
    { selector: '[data-tour="timetable-course"]', title: 'onboarding.tourTimetable', body: 'onboarding.tourCourseInput', inputRequired: true },
    { selector: '[data-tour="timetable-submit"]', title: 'onboarding.tourTimetable', body: 'onboarding.tourCourseConfirm', clickRequired: true },
    { selector: '[data-tour="timetable-page"]', title: 'onboarding.tourTimetable', body: 'onboarding.tourTimetableReady' },
    { selector: '[data-tour="calendar"]', title: 'onboarding.tourCalendar', body: 'onboarding.tourClickCalendar', clickRequired: true },
    { selector: '[data-tour="calendar-add"]', title: 'onboarding.tourCalendar', body: 'onboarding.tourCalendarBody' },
    { selector: '[data-tour="grades"]', title: 'onboarding.tourGrades', body: 'onboarding.tourClickGrades', clickRequired: true },
    { selector: '[data-tour="grades-form"]', title: 'onboarding.tourGrades', body: 'onboarding.tourGradesEntry' },
    { selector: '[data-tour="groups"]', title: 'onboarding.tourGroups', body: 'onboarding.tourClickGroups', clickRequired: true },
    { selector: '[data-tour="groups-types"]', title: 'onboarding.tourGroups', body: 'onboarding.tourGroupsTypes' },
    { selector: '[data-tour="groups-discover"]', title: 'onboarding.tourGroups', body: 'onboarding.tourDiscoverClick', clickRequired: true },
    { selector: '[data-tour="groups-discover"]', title: 'onboarding.tourGroups', body: 'onboarding.tourGroupsBody' },
    { selector: '[data-tour="news-feed"]', title: 'onboarding.tourNews', body: 'onboarding.tourNewsBody', route: '/feed' },
    { selector: '[data-tour="news-feed"]', title: 'onboarding.tourNews', body: 'onboarding.tourFinish', final: true },
  ];

  start(): void { this.stepIndex.set(0); this.inputCompleted.set(false); this.active.set(true); }
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
    this._auth.completeOnboarding().subscribe({ next: () => this._router.navigate(['/feed']) });
  }
  completeInput(): void { this.inputCompleted.set(true); }
}
