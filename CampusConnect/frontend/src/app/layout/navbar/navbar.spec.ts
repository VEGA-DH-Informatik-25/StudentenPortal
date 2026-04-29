import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { Auth } from '../../core/services/auth';
import { Navbar } from './navbar';

describe('Navbar', () => {
  let component: Navbar;
  let fixture: ComponentFixture<Navbar>;

  beforeEach(async () => {
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
    auth.displayName.set('Alice Beispiel');
    auth.userRole.set('Student');
    auth.userProfile.set({
      id: 'user-1',
      email: 'alice@dhbw-loerrach.de',
      displayName: 'Alice Beispiel',
      studyProgram: 'Informatik',
      semester: 3,
      course: 'TIF25A',
      phoneNumber: '+49 7621 123456',
      location: 'Bibliothek',
      profileNote: 'Sucht eine Projektgruppe.',
      role: 'Student',
      createdAt: '2026-04-27T10:00:00Z',
    });

    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Alice Beispiel');
    expect(text).toContain('TIF25A · 3. Semester');
    expect(text).toContain('Profil bearbeiten');
  });
});
