import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { Auth } from '../../core/services/auth';
import { I18n } from '../../core/i18n/i18n';
import { GuidedTour } from '../../core/services/guided-tour';
import { Theme } from '../../core/services/theme';
import { Navbar } from './navbar';

describe('Navbar', () => {
  let component: Navbar;
  let fixture: ComponentFixture<Navbar>;

  beforeEach(async () => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.style.colorScheme = '';

    await TestBed.configureTestingModule({
      imports: [Navbar],
      providers: [provideHttpClient(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(Navbar);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show the current profile in the top right user menu', () => {
    const auth = TestBed.inject(Auth);
    auth.displayName.set('Alice Example');
    auth.userRole.set('Student');
    auth.userProfile.set({
      id: 'user-1',
      email: 'alice@dhbw-loerrach.de',
      displayName: 'Alice Example',
      studyProgram: 'Computer Science',
      course: 'TIF25A',
      phoneNumber: '+49 7621 123456',
      location: 'Library',
      profileNote: 'Looking for a project group.',
      role: 'Student',
      mustChangePassword: false,
      onboardingCompleted: true,
      onboardingCompletedAt: null,
      createdAt: '2026-04-27T10:00:00Z',
    });

    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Alice Example');
    expect(text).toContain('TIF25A');
    expect(text).toContain('Profil bearbeiten');
  });

  it('should switch and persist the selected language', () => {
    fixture.detectChanges();

    const settingsMenu = fixture.nativeElement.querySelector('.navbar__settings-menu') as HTMLDetailsElement;
    settingsMenu.open = true;
    fixture.detectChanges();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('.navbar__choice')) as HTMLButtonElement[];
    expect(buttons.map(button => button.textContent?.trim())).toContain('Français');
    buttons.find(button => button.textContent?.trim() === 'English')?.click();
    fixture.detectChanges();

    const i18n = TestBed.inject(I18n);
    expect(i18n.language()).toBe('en');
    expect(localStorage.getItem('campusconnect.language')).toBe('en');
    expect(fixture.nativeElement.textContent).toContain('Language');
    expect(buttons.find(button => button.textContent?.trim() === 'English')?.getAttribute('aria-pressed')).toBe('true');
  });

  it('should switch and persist French from the language menu', () => {
    fixture.detectChanges();

    const settingsMenu = fixture.nativeElement.querySelector('.navbar__settings-menu') as HTMLDetailsElement;
    settingsMenu.open = true;
    fixture.detectChanges();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('.navbar__choice')) as HTMLButtonElement[];
    buttons.find(button => button.textContent?.trim() === 'Français')?.click();
    fixture.detectChanges();

    const i18n = TestBed.inject(I18n);
    expect(i18n.language()).toBe('fr');
    expect(localStorage.getItem('campusconnect.language')).toBe('fr');
    expect(fixture.nativeElement.textContent).toContain('Langue');
    expect(fixture.nativeElement.textContent).toContain('Paramètres');
    expect(buttons.find(button => button.textContent?.trim() === 'Français')?.getAttribute('aria-pressed')).toBe('true');
  });

  it('should switch and persist the selected theme preference', () => {
    fixture.detectChanges();

    const settingsMenu = fixture.nativeElement.querySelector('.navbar__settings-menu') as HTMLDetailsElement;
    settingsMenu.open = true;
    fixture.detectChanges();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('.navbar__choice')) as HTMLButtonElement[];
    buttons.find(button => button.textContent?.trim() === 'Dunkel')?.click();
    fixture.detectChanges();

    const theme = TestBed.inject(Theme);
    expect(theme.preference()).toBe('dark');
    expect(localStorage.getItem('campusconnect.theme')).toBe('dark');
    expect(document.documentElement.dataset['theme']).toBe('dark');
  });

  it('should close open menus when clicking outside them', () => {
    fixture.detectChanges();
    const settingsMenu = fixture.nativeElement.querySelector('.navbar__settings-menu') as HTMLDetailsElement;
    const profileMenu = fixture.nativeElement.querySelector('.navbar__profile-menu') as HTMLDetailsElement;

    settingsMenu.open = true;
    profileMenu.open = true;
    document.body.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(settingsMenu.open).toBe(false);
    expect(profileMenu.open).toBe(false);
  });

  it('starts the pending groups tour when groups is opened', () => {
    const guidedTour = TestBed.inject(GuidedTour);
    const startGroupsTour = vi.spyOn(guidedTour, 'startGroupsTour');
    fixture.detectChanges();

    const groupsLink = fixture.nativeElement.querySelector('[data-tour="groups"]') as HTMLAnchorElement;
    groupsLink.click();

    expect(startGroupsTour).toHaveBeenCalledOnce();
  });
});
