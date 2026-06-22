import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { Admin } from '../../../core/services/admin';
import { Auth } from '../../../core/services/auth';
import { AdminPage } from './admin-page';

describe('AdminPage', () => {
  let component: AdminPage;
  let fixture: ComponentFixture<AdminPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminPage],
      providers: [
        {
          provide: Admin,
          useValue: {
            getUsers: () => of([]),
            getCourses: () => of([]),
            createCourse: () => of({ code: 'TIF25A', studyProgram: 'Computer Science', isActive: true, createdAt: '' }),
            createUser: () => of({}),
            updateUser: () => of({}),
            updateUserStatus: () => of({}),
            updateUserRole: () => of({}),
            updateUserCourse: () => of({}),
            deleteUser: () => of(undefined),
          },
        },
        {
          provide: Auth,
          useValue: {
            userProfile: () => null,
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('generates a secure initial password', () => {
    const adminPage = component as unknown as {
      generateInitialPassword(): void;
      _createForm: { initialPassword: string };
    };
    adminPage.generateInitialPassword();

    const password = adminPage._createForm.initialPassword;
    expect(password).toHaveLength(20);
    expect(password).toMatch(/[A-Z]/);
    expect(password).toMatch(/[a-z]/);
    expect(password).toMatch(/[0-9]/);
    expect(password).toMatch(/[!@#$%&*+\-_=]/);
  });
});
