import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { Auth } from './auth';
import { GuidedTour } from './guided-tour';

describe('GuidedTour', () => {
  const profile = {
    id: 'user-1',
    email: 'student@dhbw-loerrach.de',
    displayName: 'Student',
    studyProgram: 'Computer Science',
    course: 'TIF25A',
    phoneNumber: '',
    location: '',
    role: 'Student' as const,
    mustChangePassword: false,
    onboardingCompleted: true,
    onboardingCompletedAt: '2026-07-04T10:00:00Z',
    createdAt: '2026-07-04T09:00:00Z',
  };

  function setup() {
    localStorage.clear();
    const auth = {
      userProfile: signal(profile),
      completeOnboarding: vi.fn(() => of(profile)),
    };
    const router = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        GuidedTour,
        { provide: Auth, useValue: auth },
        { provide: Router, useValue: router },
      ],
    });

    return { tour: TestBed.inject(GuidedTour), auth, router };
  }

  it('keeps the initial tour at eight steps and labels the last step as links', () => {
    const { tour } = setup();

    tour.start();

    expect(tour.steps).toHaveLength(8);
    expect(tour.steps[7].title).toBe('onboarding.tourLinks');
    expect(tour.steps[7].selector).toBe('[data-tour="quick-access"]');
    expect(tour.steps[7].final).toBe(true);
  });

  it('starts the existing groups explanation only after onboarding and a groups click', () => {
    const { tour, auth, router } = setup();

    tour.startGroupsTour();
    expect(tour.active()).toBe(false);

    tour.start();
    tour.stepIndex.set(7);
    tour.next();

    expect(auth.completeOnboarding).toHaveBeenCalledOnce();
    expect(router.navigate).toHaveBeenCalledWith(['/feed']);
    expect(tour.active()).toBe(false);

    tour.startGroupsTour();

    expect(tour.active()).toBe(true);
    expect(tour.steps.map(step => step.selector)).toEqual([
      '[data-tour="groups-types"]',
      '[data-tour="groups-discover"]',
      '[data-tour="groups-discover"]',
    ]);
  });

  it('shows the groups explanation only once', () => {
    const { tour } = setup();

    tour.start();
    tour.stepIndex.set(7);
    tour.next();
    tour.startGroupsTour();
    tour.finish();
    tour.startGroupsTour();

    expect(tour.active()).toBe(false);
  });
});
