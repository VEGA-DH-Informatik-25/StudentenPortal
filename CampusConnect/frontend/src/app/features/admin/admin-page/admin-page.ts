import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { I18n } from '../../../core/i18n/i18n';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { AdminCourse, AdminUser } from '../../../core/models/admin.model';
import { Admin } from '../../../core/services/admin';
import { Auth } from '../../../core/services/auth';

type AdminTab = 'overview' | 'users' | 'courses';
type EditorMode = 'create' | 'edit';
type StatusFilter = 'All' | 'Active' | 'Inactive';

const PASSWORD_LENGTH = 20;
const PASSWORD_CHARACTER_SETS = [
  'ABCDEFGHJKLMNPQRSTUVWXYZ',
  'abcdefghijkmnopqrstuvwxyz',
  '23456789',
  '!@#$%&*+-_=',
];

@Component({
  selector: 'app-admin-page',
  standalone: true,
  imports: [DatePipe, FormsModule, TranslatePipe],
  templateUrl: './admin-page.html',
  styleUrl: './admin-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminPage implements OnInit {
  private readonly _adminService = inject(Admin);
  private readonly _auth = inject(Auth);
  protected readonly _i18n = inject(I18n);

  protected readonly _users = signal<AdminUser[]>([]);
  protected readonly _courses = signal<AdminCourse[]>([]);
  protected readonly _activeTab = signal<AdminTab>('overview');
  protected readonly _isLoading = signal(false);
  protected readonly _coursesLoading = signal(false);
  protected readonly _isCreatingCourse = signal(false);
  protected readonly _isSaving = signal(false);
  protected readonly _isChangingStatus = signal(false);
  protected readonly _error = signal<string | null>(null);
  protected readonly _success = signal<string | null>(null);
  protected readonly _searchQuery = signal('');
  protected readonly _roleFilter = signal('All');
  protected readonly _courseFilter = signal('All');
  protected readonly _statusFilter = signal<StatusFilter>('All');
  protected readonly _editorMode = signal<EditorMode | null>(null);
  protected readonly _editingUser = signal<AdminUser | null>(null);
  protected readonly _statusConfirmationOpen = signal(false);
  protected readonly _roles = ['Student', 'Lecturer', 'Management', 'Admin'];
  protected readonly _roleCourseCodes = new Map<string, string>([
    ['Lecturer', 'LECTURER'],
    ['Management', 'MANAGEMENT'],
    ['Admin', 'ADMIN'],
  ]);

  protected readonly _courseForm = {
    code: '',
    studyProgram: '',
  };

  protected readonly _createForm = {
    firstName: '',
    lastName: '',
    email: '',
    role: 'Student',
    courseCode: '',
    initialPassword: '',
    isActive: true,
  };

  protected readonly _editForm = {
    displayName: '',
    email: '',
    role: 'Student',
    courseCode: '',
    isActive: true,
  };

  protected readonly _dashboardStats = computed(() => {
    const users = this._users();
    return {
      totalUsers: users.length,
      activeUsers: users.filter(user => user.isActive).length,
      inactiveUsers: users.filter(user => !user.isActive).length,
      courses: this._courses().length,
      admins: users.filter(user => user.role === 'Admin').length,
      students: users.filter(user => user.role === 'Student').length,
    };
  });

  protected readonly _filteredUsers = computed(() => {
    const searchQuery = this._normalize(this._searchQuery());
    const roleFilter = this._roleFilter();
    const courseFilter = this._courseFilter();
    const statusFilter = this._statusFilter();

    return this._users().filter(user => {
      const matchesSearch = !searchQuery ||
        this._normalize(`${user.displayName} ${user.email} ${user.studyProgram} ${user.course}`).includes(searchQuery);
      const matchesRole = roleFilter === 'All' || user.role === roleFilter;
      const matchesCourse = courseFilter === 'All' || user.course === courseFilter;
      const matchesStatus = statusFilter === 'All' ||
        (statusFilter === 'Active' && user.isActive) ||
        (statusFilter === 'Inactive' && !user.isActive);
      return matchesSearch && matchesRole && matchesCourse && matchesStatus;
    });
  });

  protected readonly _courseRows = computed(() => this._courses().map(course => ({
    course,
    userCount: this._users().filter(user => user.course === course.code).length,
  })));

  ngOnInit(): void {
    this.loadData();
  }

  protected loadData(): void {
    this.loadUsers();
    this.loadCourses();
  }

  protected loadUsers(): void {
    this._isLoading.set(true);
    this._error.set(null);
    this._adminService.getUsers().subscribe({
      next: users => {
        this._users.set(users);
        this._isLoading.set(false);
      },
      error: error => {
        this._error.set(this._readError(error));
        this._isLoading.set(false);
      },
    });
  }

  protected loadCourses(): void {
    this._coursesLoading.set(true);
    this._adminService.getCourses().subscribe({
      next: courses => {
        this._courses.set(courses);
        this._setDefaultCourse();
        this._coursesLoading.set(false);
      },
      error: error => {
        this._error.set(this._readError(error));
        this._coursesLoading.set(false);
      },
    });
  }

  protected switchTab(tab: AdminTab): void {
    this._activeTab.set(tab);
  }

  protected openCreateFromDashboard(): void {
    this.switchTab('users');
    this.openCreateUser();
  }

  protected createCourse(): void {
    const code = this._courseForm.code.trim();
    const studyProgram = this._courseForm.studyProgram.trim();
    if (!code || !studyProgram) {
      this._error.set(this._i18n.translate('admin.courseFieldsRequired'));
      return;
    }

    this._isCreatingCourse.set(true);
    this._clearMessages();
    this._adminService.createCourse({ code, studyProgram }).subscribe({
      next: course => {
        this._courses.update(courses => [...courses.filter(item => item.code !== course.code), course].sort((a, b) => a.code.localeCompare(b.code)));
        this._courseForm.code = '';
        this._courseForm.studyProgram = '';
        this._setDefaultCourse();
        this._success.set(this._i18n.translate('admin.courseCreated', { code: course.code }));
        this._isCreatingCourse.set(false);
      },
      error: error => {
        this._error.set(this._readError(error));
        this._isCreatingCourse.set(false);
      },
    });
  }

  protected updateSearchQuery(value: string): void {
    this._searchQuery.set(value);
  }

  protected updateRoleFilter(value: string): void {
    this._roleFilter.set(value);
  }

  protected updateCourseFilter(value: string): void {
    this._courseFilter.set(value);
  }

  protected updateStatusFilter(value: StatusFilter): void {
    this._statusFilter.set(value);
  }

  protected openCreateUser(): void {
    this._clearMessages();
    this._statusConfirmationOpen.set(false);
    this._editingUser.set(null);
    this._createForm.firstName = '';
    this._createForm.lastName = '';
    this._createForm.email = '';
    this._createForm.role = 'Student';
    this._createForm.courseCode = this._defaultCourseForRole('Student');
    this._createForm.initialPassword = '';
    this._createForm.isActive = true;
    this._editorMode.set('create');
  }

  protected updateCreateRole(role: string): void {
    this._createForm.role = role;
    this._createForm.courseCode = this._defaultCourseForRole(role);
  }

  protected generateInitialPassword(): void {
    const passwordCharacters = [
      ...PASSWORD_CHARACTER_SETS.map(characterSet => this._randomCharacter(characterSet)),
    ];
    const allCharacters = PASSWORD_CHARACTER_SETS.join('');

    while (passwordCharacters.length < PASSWORD_LENGTH) {
      passwordCharacters.push(this._randomCharacter(allCharacters));
    }

    for (let index = passwordCharacters.length - 1; index > 0; index--) {
      const randomIndex = this._randomIndex(index + 1);
      [passwordCharacters[index], passwordCharacters[randomIndex]] = [passwordCharacters[randomIndex], passwordCharacters[index]];
    }

    this._createForm.initialPassword = passwordCharacters.join('');
  }

  protected updateEditRole(role: string): void {
    this._editForm.role = role;
    this._editForm.courseCode = this._defaultCourseForRole(role) || this._editForm.courseCode;
  }

  protected openEditUser(user: AdminUser): void {
    this._clearMessages();
    this._statusConfirmationOpen.set(false);
    this._editingUser.set(user);
    this._editForm.displayName = user.displayName;
    this._editForm.email = user.email;
    this._editForm.role = user.role;
    this._editForm.courseCode = user.course || this._firstCourseCode();
    this._editForm.isActive = user.isActive;
    this._editorMode.set('edit');
  }

  protected closeEditor(): void {
    if (this._isSaving() || this._isChangingStatus()) {
      return;
    }

    this._editorMode.set(null);
    this._editingUser.set(null);
    this._statusConfirmationOpen.set(false);
  }

  protected createUser(): void {
    const validationError = this._validateCreateForm();
    if (validationError) {
      this._error.set(validationError);
      return;
    }

    this._isSaving.set(true);
    this._clearMessages();
    this._adminService.createUser({
      firstName: this._createForm.firstName.trim(),
      lastName: this._createForm.lastName.trim(),
      email: this._createForm.email.trim(),
      role: this._createForm.role,
      courseCode: this._createForm.courseCode,
      initialPassword: this._createForm.initialPassword,
      isActive: this._createForm.isActive,
    }).subscribe({
      next: user => {
        this._users.update(users => this._sortUsers([...users, user]));
        this._success.set(this._i18n.translate('admin.userCreated', { name: user.displayName }));
        this._isSaving.set(false);
        this.closeEditor();
        this.loadUsers();
      },
      error: error => {
        this._error.set(this._readError(error));
        this._isSaving.set(false);
      },
    });
  }

  protected saveUser(): void {
    const user = this._editingUser();
    if (!user) {
      return;
    }

    const validationError = this._validateEditForm();
    if (validationError) {
      this._error.set(validationError);
      return;
    }

    this._isSaving.set(true);
    this._clearMessages();
    this._adminService.updateUser(user.id, {
      displayName: this._editForm.displayName.trim(),
      email: this._editForm.email.trim(),
      role: this._editForm.role,
      courseCode: this._editForm.courseCode,
      isActive: this._editForm.isActive,
    }).subscribe({
      next: updatedUser => {
        this._users.update(users => users.map(item => item.id === updatedUser.id ? updatedUser : item));
        this._editingUser.set(updatedUser);
        this._success.set(this._i18n.translate('admin.userUpdated', { name: updatedUser.displayName }));
        this._isSaving.set(false);
        this.closeEditor();
        this.loadUsers();
      },
      error: error => {
        this._error.set(this._readError(error));
        this._isSaving.set(false);
      },
    });
  }

  protected requestDeactivateUser(): void {
    this._statusConfirmationOpen.set(true);
  }

  protected closeStatusConfirmation(): void {
    this._statusConfirmationOpen.set(false);
  }

  protected reactivateUser(): void {
    this.updateSelectedUserStatus(true);
  }

  protected deactivateConfirmedUser(): void {
    this.updateSelectedUserStatus(false);
  }

  protected updateSelectedUserStatus(isActive: boolean): void {
    const user = this._editingUser();
    if (!user) {
      return;
    }

    this._isChangingStatus.set(true);
    this._clearMessages();
    this._adminService.updateUserStatus(user.id, isActive).subscribe({
      next: updatedUser => {
        this._users.update(users => users.map(item => item.id === updatedUser.id ? updatedUser : item));
        this._editingUser.set(updatedUser);
        this._editForm.isActive = updatedUser.isActive;
        this._success.set(this._i18n.translate(isActive ? 'admin.userReactivated' : 'admin.userDeactivated', { name: updatedUser.displayName }));
        this._isChangingStatus.set(false);
        this._statusConfirmationOpen.set(false);
        this.closeEditor();
        this.loadUsers();
      },
      error: error => {
        this._error.set(this._readError(error));
        this._isChangingStatus.set(false);
      },
    });
  }

  protected courseLabel(course: AdminCourse): string {
    return `${course.code} - ${course.studyProgram}`;
  }

  protected roleLabel(role: string): string {
    return this._i18n.roleLabel(role);
  }

  protected statusLabel(isActive: boolean): string {
    return this._i18n.translate(isActive ? 'admin.statusActive' : 'admin.statusInactive');
  }

  protected courseExists(courseCode: string): boolean {
    return this._courses().some(course => course.code === courseCode);
  }

  protected isEditingCurrentUser(): boolean {
    const editingUser = this._editingUser();
    const currentUserId = this._auth.userProfile()?.id;
    return !!editingUser && !!currentUserId && editingUser.id === currentUserId;
  }

  private _validateCreateForm(): string | null {
    if (!this._createForm.firstName.trim() || !this._createForm.lastName.trim() || !this._createForm.email.trim()) {
      return this._i18n.translate('admin.userFieldsRequired');
    }

    if (!this._isValidEmail(this._createForm.email)) {
      return this._i18n.translate('admin.userEmailInvalid');
    }

    if (!this._roles.includes(this._createForm.role)) {
      return this._i18n.translate('admin.roleRequired');
    }

    if (!this._createForm.courseCode) {
      return this._i18n.translate('admin.courseRequired');
    }

    if (!this._createForm.initialPassword.trim()) {
      return this._i18n.translate('admin.passwordRequired');
    }

    return null;
  }

  private _validateEditForm(): string | null {
    if (!this._editForm.displayName.trim() || !this._editForm.email.trim()) {
      return this._i18n.translate('admin.userFieldsRequired');
    }

    if (!this._isValidEmail(this._editForm.email)) {
      return this._i18n.translate('admin.userEmailInvalid');
    }

    if (!this._roles.includes(this._editForm.role)) {
      return this._i18n.translate('admin.roleRequired');
    }

    if (!this._editForm.courseCode) {
      return this._i18n.translate('admin.courseRequired');
    }

    return null;
  }

  private _isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
  }

  private _randomCharacter(characters: string): string {
    return characters[this._randomIndex(characters.length)];
  }

  private _randomIndex(upperBound: number): number {
    const limit = Math.floor(0x1_0000_0000 / upperBound) * upperBound;
    const values = new Uint32Array(1);
    let value: number;

    do {
      crypto.getRandomValues(values);
      value = values[0];
    } while (value >= limit);

    return value % upperBound;
  }

  private _setDefaultCourse(): void {
    const firstCourseCode = this._defaultCourseForRole('Student');
    if (!this._createForm.courseCode) {
      this._createForm.courseCode = firstCourseCode;
    }

    if (!this._editForm.courseCode) {
      this._editForm.courseCode = firstCourseCode;
    }
  }

  private _firstCourseCode(): string {
    return this._courses().find(course => !this._roleCourseCodes.has(course.code))?.code ?? this._courses()[0]?.code ?? '';
  }

  private _defaultCourseForRole(role: string): string {
    const roleCourseCode = this._roleCourseCodes.get(role);
    if (roleCourseCode && this.courseExists(roleCourseCode)) {
      return roleCourseCode;
    }

    return this._firstCourseCode();
  }

  private _normalize(value: string): string {
    return value.trim().toLowerCase();
  }

  private _sortUsers(users: AdminUser[]): AdminUser[] {
    return users.sort((a, b) => a.displayName.localeCompare(b.displayName));
  }

  private _clearMessages(): void {
    this._error.set(null);
    this._success.set(null);
  }

  private _readError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as { error?: string } | null;
      return body?.error ?? this._i18n.translate('admin.dataLoadError');
    }

    return this._i18n.translate('admin.dataLoadError');
  }
}
