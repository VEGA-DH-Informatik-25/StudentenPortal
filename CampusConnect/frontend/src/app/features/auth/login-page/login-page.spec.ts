import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { Auth } from '../../../core/services/auth';
import { Courses } from '../../../core/services/courses';
import { LoginPage } from './login-page';

describe('LoginPage', () => {
  let component: LoginPage;
  let fixture: ComponentFixture<LoginPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        provideRouter([]),
        {
          provide: Auth,
          useValue: {
            login: vi.fn(() => of({ token: 'token', email: 'alice@dhbw-loerrach.de', displayName: 'Alice', role: 'Student' })),
            register: vi.fn(() => of({ token: 'token', email: 'alice@dhbw-loerrach.de', displayName: 'Alice', role: 'Student' })),
          },
        },
        {
          provide: Courses,
          useValue: {
            getCourses: vi.fn(() => of([{ code: 'TIF25A', studyProgram: 'Informatik', semester: 2 }])),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should preselect the first available course for registration', () => {
    expect(component['_registerForm'].course).toBe('TIF25A');
  });
});