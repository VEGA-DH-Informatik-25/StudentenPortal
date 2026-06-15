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
            createCourse: () => of({ code: 'TIF25A', studyProgram: 'Computer Science', semester: 3, isActive: true, createdAt: '' }),
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
});
