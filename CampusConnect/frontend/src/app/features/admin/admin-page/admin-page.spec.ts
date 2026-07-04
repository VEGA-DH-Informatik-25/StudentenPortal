import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { Admin } from '../../../core/services/admin';
import { Auth } from '../../../core/services/auth';
import { AdminPage } from './admin-page';

describe('AdminPage', () => {
  let component: AdminPage;
  let fixture: ComponentFixture<AdminPage>;

  beforeEach(async () => {
    localStorage.clear();
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
            resetUserPassword: () => of({}),
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

  it('persists admin tabs and filters', () => {
    (component as any).switchTab('users');
    (component as any).updateRoleFilter('Lecturer');
    (component as any).updateCourseFilter('TIF25A');
    (component as any).updateStatusFilter('Active');

    expect(localStorage.getItem('campusconnect.admin.activeTab')).toBe('users');
    expect(localStorage.getItem('campusconnect.admin.roleFilter')).toBe('Lecturer');
    expect(localStorage.getItem('campusconnect.admin.courseFilter')).toBe('TIF25A');
    expect(localStorage.getItem('campusconnect.admin.statusFilter')).toBe('Active');
  });

  it('restores admin tabs and filters', async () => {
    fixture.destroy();
    localStorage.setItem('campusconnect.admin.activeTab', 'courses');
    localStorage.setItem('campusconnect.admin.roleFilter', 'Management');
    localStorage.setItem('campusconnect.admin.courseFilter', 'WWI25A');
    localStorage.setItem('campusconnect.admin.statusFilter', 'Inactive');

    fixture = TestBed.createComponent(AdminPage);
    component = fixture.componentInstance;
    await fixture.whenStable();

    expect((component as any)._activeTab()).toBe('courses');
    expect((component as any)._roleFilter()).toBe('Management');
    expect((component as any)._courseFilter()).toBe('WWI25A');
    expect((component as any)._statusFilter()).toBe('Inactive');
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

  it('resets a selected user password', () => {
    const adminService = TestBed.inject(Admin) as unknown as { resetUserPassword: ReturnType<typeof vi.fn> };
    adminService.resetUserPassword = vi.fn(() => of({
      id: 'user-1',
      displayName: 'Alice Example',
      email: 'alice@dhbw-loerrach.de',
      studyProgram: 'Computer Science',
      course: 'TIF25A',
      role: 'Student',
      isActive: true,
      createdAt: '2026-04-27T10:00:00Z',
    }));
    const adminPage = component as any;
    adminPage.openEditUser({
      id: 'user-1',
      displayName: 'Alice Example',
      email: 'alice@dhbw-loerrach.de',
      studyProgram: 'Computer Science',
      course: 'TIF25A',
      role: 'Student',
      isActive: true,
      createdAt: '2026-04-27T10:00:00Z',
    });
    adminPage._passwordResetForm.initialPassword = 'ResetStart123!';

    adminPage.resetPassword();

    expect(adminService.resetUserPassword).toHaveBeenCalledWith('user-1', 'ResetStart123!');
  });
});
