import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { Admin } from '../../../core/services/admin';
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
            createCourse: () => of({ code: 'TIF25A', studyProgram: 'Informatik', semester: 3, isActive: true, createdAt: '' }),
            updateUserRole: () => of({}),
            updateUserCourse: () => of({}),
            deleteUser: () => of(undefined),
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
