import { registerLocaleData } from '@angular/common';
import localeDe from '@angular/common/locales/de';
import localeEn from '@angular/common/locales/en';
import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { CampusGroup } from '../models/group.model';
import { LanguageCode, TranslationKey, translations } from './translations';

export interface LanguageOption {
  code: LanguageCode;
  label: string;
  shortLabel: string;
  locale: string;
}

const STORAGE_KEY = 'campusconnect.language';

registerLocaleData(localeDe);
registerLocaleData(localeEn);

const apiErrorTranslations: Record<string, TranslationKey> = {
  'A course group already exists for this course.': 'apiError.courseGroupExists',
  'Audience must be at most 80 characters long.': 'apiError.groupAudienceTooLong',
  'Change the initial password before completing onboarding.': 'apiError.changeInitialPasswordFirst',
  'Choose a course.': 'apiError.courseRequired',
  'Choose a valid course.': 'apiError.invalidCourse',
  'Choose a valid emoji.': 'apiError.validEmoji',
  'Choose a valid group.': 'apiError.validGroup',
  'Comment cannot be empty.': 'apiError.commentEmpty',
  'Comment was not found.': 'apiError.commentNotFound',
  'Comments are closed in this group.': 'apiError.commentsClosed',
  'Content cannot be empty.': 'apiError.contentEmpty',
  'Course already exists.': 'apiError.courseAlreadyExists',
  'Course code must be at most 40 characters long.': 'apiError.courseCodeTooLong',
  'Course group membership is managed through course assignments.': 'apiError.courseManagedGroup',
  'Description must be at most 240 characters long.': 'apiError.descriptionTooLong',
  'Display name is required.': 'apiError.displayNameRequired',
  'Display name must be at most 120 characters long.': 'apiError.displayNameTooLong',
  'ECTS points must be greater than 0.': 'apiError.ectsPositive',
  'Email address is required.': 'apiError.emailRequired',
  'Enter a course code for the course group.': 'apiError.courseCodeRequired',
  'Enter a valid email address.': 'apiError.emailInvalid',
  'Enter an official category for the official group.': 'apiError.officialCategoryRequired',
  'Fill in all course fields.': 'apiError.courseFieldsRequired',
  'Fill in all group fields.': 'apiError.groupFieldsRequired',
  'Fill in all profile fields.': 'apiError.profileFieldsRequired',
  'First name and last name are required.': 'apiError.firstLastRequired',
  'First name and last name must be at most 60 characters long.': 'apiError.firstLastTooLong',
  'Grade must be between 1.0 and 5.0.': 'apiError.gradeRange',
  'Group deletion is not available.': 'apiError.groupDeletionUnavailable',
  'Group name must be at most 80 characters long.': 'apiError.groupNameTooLong',
  'Group role is invalid.': 'apiError.groupRoleInvalid',
  'Group type is invalid.': 'apiError.groupTypeInvalid',
  'Group was not found.': 'apiError.groupNotFound',
  'Initial password is required.': 'apiError.initialPasswordRequired',
  'Invalid email address or password.': 'apiError.invalidCredentials',
  'Join rule is invalid.': 'apiError.joinRuleInvalid',
  'Location must be at most 120 characters long.': 'apiError.locationTooLong',
  'Members are not allowed to publish posts in this group.': 'apiError.membersCannotPublish',
  'Module name cannot be empty.': 'apiError.moduleNameRequired',
  'Modulname darf nicht leer sein.': 'apiError.examModuleRequired',
  'New owner profile was not found.': 'apiError.newOwnerNotFound',
  'No accounts are assigned to this course.': 'apiError.noCourseAccounts',
  'No valid course is assigned to your profile.': 'apiError.noValidCourseProfile',
  'Official category must be at most 80 characters long.': 'apiError.officialCategoryTooLong',
  'Only @dhbw-loerrach.de email addresses are allowed.': 'apiError.emailDomain',
  'Only the group owner can appoint moderators.': 'apiError.ownerOnlyModerator',
  'Password must be at least 8 characters long.': 'apiError.passwordTooShort',
  'Password must contain uppercase and lowercase letters, a number, and a special character.': 'apiError.passwordComplexity',
  'Permission denied.': 'apiError.permissionDenied',
  'Phone number must be at most 40 characters long.': 'apiError.phoneTooLong',
  'Post was not found.': 'apiError.postNotFound',
  'Profile note must be at most 280 characters long.': 'apiError.profileNoteTooLong',
  'Select a course to add.': 'apiError.selectCourseAdd',
  'Select a new owner before leaving this group.': 'apiError.selectNewOwner',
  'Select an existing group member as the new owner.': 'apiError.selectExistingOwner',
  'Select at least one account that is not already a member.': 'apiError.selectInvitees',
  'Select at least one valid account to add.': 'apiError.selectAccounts',
  'Study program must be at most 120 characters long.': 'apiError.studyProgramTooLong',
  'The current password is incorrect.': 'apiError.currentPasswordIncorrect',
  'The group owner cannot be removed.': 'apiError.ownerCannotBeRemoved',
  'The group owner role cannot be changed.': 'apiError.ownerRoleLocked',
  'Too many login attempts. Please try again later.': 'apiError.loginRateLimited',
  'There is no pending invitation for this account.': 'apiError.noPendingInvitation',
  'There is no pending join request for this account.': 'apiError.noPendingJoinRequest',
  'This account is not a member of the group.': 'apiError.accountNotMember',
  'This course already exists.': 'apiError.courseAlreadyExists',
  'This email address is already registered.': 'apiError.emailAlreadyRegistered',
  'This global role cannot create this group type.': 'apiError.globalRoleCannotCreateGroup',
  'This post is not waiting for approval.': 'apiError.postNotWaitingApproval',
  'This post is waiting for approval.': 'apiError.postWaitingApproval',
  'This role is invalid.': 'apiError.invalidRole',
  'User could not be resolved from the token.': 'apiError.userNotResolved',
  'User profile was not found.': 'apiError.userProfileNotFound',
  'User was not found.': 'apiError.userNotFound',
  'You are not a member of this group.': 'apiError.notGroupMember',
  'You are not allowed to manage this group.': 'apiError.groupPermissionDenied',
  'You can only post in groups assigned to you.': 'apiError.onlyAssignedGroups',
  'You cannot deactivate your own admin account.': 'apiError.selfAdminDeactivate',
  'You cannot delete your own admin account.': 'apiError.selfAdminDelete',
  'You cannot join this group directly.': 'apiError.youCannotJoinDirectly',
  'You cannot remove your own admin role.': 'apiError.selfAdminRole',
  'You do not have a pending invitation for this group.': 'apiError.noPendingInvitationForGroup',
  'Your join request is already pending.': 'apiError.joinRequestPending',
};

@Injectable({ providedIn: 'root' })
export class I18n {
  readonly languages: LanguageOption[] = [
    { code: 'de', label: 'Deutsch', shortLabel: 'DE', locale: 'de-DE' },
    { code: 'en', label: 'English', shortLabel: 'EN', locale: 'en-US' },
  ];

  private readonly _language = signal<LanguageCode>(this._initialLanguage());
  readonly language = this._language.asReadonly();

  constructor() {
    this._applyDocumentLanguage(this._language());
  }

  setLanguage(language: string): void {
    const normalizedLanguage = this._normalizeLanguageCode(language);
    if (!normalizedLanguage) {
      return;
    }

    this._language.set(normalizedLanguage);
    this._applyDocumentLanguage(normalizedLanguage);
    globalThis.localStorage?.setItem(STORAGE_KEY, normalizedLanguage);
  }

  locale(): string {
    return this.languages.find(language => language.code === this._language())?.locale ?? 'de-DE';
  }

  translate(key: TranslationKey, params: Record<string, string | number> = {}): string {
    const template = translations[this._language()][key] ?? translations.en[key] ?? key;
    return template.replace(/{{\s*(\w+)\s*}}/g, (_, token: string) => String(params[token] ?? ''));
  }

  readError(error: unknown, fallbackKey: TranslationKey): string {
    const backendMessage = this._extractBackendError(error);
    const key = backendMessage ? apiErrorTranslations[backendMessage] : null;
    return this.translate(key ?? fallbackKey);
  }

  roleLabel(role: string): string {
    switch (role) {
      case 'Admin':
        return this.translate('role.admin');
      case 'Lecturer':
        return this.translate('role.lecturer');
      case 'Management':
        return this.translate('role.management');
      case 'Student':
      default:
        return this.translate('role.student');
    }
  }

  groupRoleLabel(role: string): string {
    switch (role) {
      case 'Owner':
        return this.translate('groups.role.owner');
      case 'Moderator':
        return this.translate('groups.role.moderator');
      case 'Member':
        return this.translate('groups.role.member');
      case 'None':
      default:
        return this.translate('groups.role.none');
    }
  }

  groupName(group: CampusGroup): string {
    if (group.type === 'Course' && group.courseCode && this._isGeneratedCourseName(group.name, group.courseCode)) {
      return this.translate('groups.name.course', { code: group.courseCode });
    }

    const key = this._groupNameKey(group.name);
    return key ? this.translate(key) : group.name;
  }

  groupDescription(group: CampusGroup): string {
    if (this._isGeneratedCourseDescription(group.description)) {
      return this.translate('groups.description.course');
    }

    const nameKey = this._groupNameKey(group.name);
    const descriptionKey = nameKey ? this._groupDescriptionKey(nameKey) : null;
    return descriptionKey ? this.translate(descriptionKey) : group.description;
  }

  groupAudience(group: CampusGroup): string {
    if (group.type === 'Course' && group.courseCode) {
      return group.courseCode;
    }

    const key = this._groupAudienceKey(group.audience);
    return key ? this.translate(key) : group.audience;
  }

  groupOwnerLabel(group: CampusGroup): string {
    const key = this._groupOwnerKey(group.ownerLabel);
    return key ? this.translate(key) : group.ownerLabel;
  }

  private _initialLanguage(): LanguageCode {
    const storedLanguage = globalThis.localStorage?.getItem(STORAGE_KEY);
    const normalizedLanguage = this._normalizeLanguageCode(storedLanguage);
    if (normalizedLanguage) {
      if (storedLanguage !== normalizedLanguage) {
        globalThis.localStorage?.setItem(STORAGE_KEY, normalizedLanguage);
      }

      return normalizedLanguage;
    }

    if (storedLanguage) {
      globalThis.localStorage?.removeItem(STORAGE_KEY);
    }

    return 'de';
  }

  private _normalizeLanguageCode(language: string | null | undefined): LanguageCode | null {
    const normalizedLanguage = language?.trim().toLowerCase() ?? '';
    return this._isLanguageCode(normalizedLanguage) ? normalizedLanguage : null;
  }

  private _isLanguageCode(language: string): language is LanguageCode {
    return language === 'en' || language === 'de';
  }

  private _applyDocumentLanguage(language: LanguageCode): void {
    const root = globalThis.document?.documentElement;
    if (root) {
      root.lang = language;
    }
  }

  private _extractBackendError(error: unknown): string | null {
    if (!(error instanceof HttpErrorResponse)) {
      return null;
    }

    const body = error.error;
    if (typeof body === 'string') {
      return body;
    }

    if (body && typeof body === 'object' && 'error' in body) {
      const message = (body as { error?: unknown }).error;
      return typeof message === 'string' ? message : null;
    }

    return null;
  }

  private _groupNameKey(name: string): TranslationKey | null {
    switch (this._normalizeLabel(name)) {
      case 'official announcements':
      case 'offizielle mitteilungen':
        return 'groups.name.officialAnnouncements';
      case 'exam office and deadlines':
      case 'prüfungsamt und fristen':
      case 'pruefungsamt und fristen':
        return 'groups.name.examOffice';
      case 'mensa and hangstrasse campus':
      case 'mensa und campus hangstraße':
      case 'mensa und campus hangstrasse':
        return 'groups.name.mensaCampus';
      case 'library and research':
      case 'bibliothek und recherche':
        return 'groups.name.libraryResearch';
      case 'stuv, events, and university activities':
      case 'stuv, veranstaltungen und hochschulaktivitäten':
      case 'stuv, events und hochschulaktivitäten':
        return 'groups.name.stuvEvents';
      case 'housing in lörrach':
      case 'housing in loerrach':
      case 'wohnungssuche lörrach':
      case 'wohnungssuche loerrach':
        return 'groups.name.housing';
      case 'tech projects and labs':
      case 'technikprojekte und labore':
        return 'groups.name.techProjects';
      case 'moodle, webmail, and campus app help':
      case 'moodle, webmail und campus-app-hilfe':
      case 'moodle, webmail und campus app hilfe':
        return 'groups.name.moodleHelp';
      default:
        return null;
    }
  }

  private _groupDescriptionKey(nameKey: TranslationKey): TranslationKey | null {
    switch (nameKey) {
      case 'groups.name.officialAnnouncements':
        return 'groups.description.officialAnnouncements';
      case 'groups.name.examOffice':
        return 'groups.description.examOffice';
      case 'groups.name.mensaCampus':
        return 'groups.description.mensaCampus';
      case 'groups.name.libraryResearch':
        return 'groups.description.libraryResearch';
      case 'groups.name.stuvEvents':
        return 'groups.description.stuvEvents';
      case 'groups.name.housing':
        return 'groups.description.housing';
      case 'groups.name.techProjects':
        return 'groups.description.techProjects';
      case 'groups.name.moodleHelp':
        return 'groups.description.moodleHelp';
      default:
        return null;
    }
  }

  private _groupAudienceKey(audience: string): TranslationKey | null {
    switch (this._normalizeLabel(audience)) {
      case 'all students':
      case 'alle studierenden':
        return 'groups.audience.allStudents';
      case 'across study programs':
      case 'studiengangsübergreifend':
      case 'studiengangsuebergreifend':
        return 'groups.audience.acrossStudyPrograms';
      case 'hangstrasse campus':
      case 'campus hangstraße':
      case 'campus hangstrasse':
        return 'groups.audience.hangstrasseCampus';
      case 'all study programs':
      case 'alle studiengänge':
      case 'alle studiengaenge':
        return 'groups.audience.allStudyPrograms';
      case 'students in and around lörrach':
      case 'students in and around loerrach':
      case 'studierende in und um lörrach':
      case 'studierende in und um loerrach':
        return 'groups.audience.studentsLoerrach';
      case 'technology and computer science':
      case 'technik und informatik':
        return 'groups.audience.technologyComputerScience';
      case 'all accounts':
      case 'alle accounts':
        return 'groups.audience.allAccounts';
      default:
        return null;
    }
  }

  private _groupOwnerKey(ownerLabel: string): TranslationKey | null {
    switch (this._normalizeLabel(ownerLabel)) {
      case 'dhbw lörrach':
      case 'dhbw loerrach':
        return 'groups.owner.dhbwLoerrach';
      case 'exam office':
      case 'prüfungsamt':
      case 'pruefungsamt':
        return 'groups.owner.examOffice';
      case 'campusservice':
      case 'campus service':
        return 'groups.owner.campusService';
      case 'library':
      case 'bibliothek':
        return 'groups.owner.library';
      default:
        return null;
    }
  }

  private _normalizeLabel(value: string): string {
    return value.trim().replace(/\s+/g, ' ').toLowerCase();
  }

  private _isGeneratedCourseName(name: string, courseCode: string): boolean {
    const normalizedName = this._normalizeLabel(name);
    const normalizedCode = this._normalizeLabel(courseCode);
    return normalizedName === `course ${normalizedCode}` || normalizedName === `kurs ${normalizedCode}`;
  }

  private _isGeneratedCourseDescription(description: string): boolean {
    const normalizedDescription = this._normalizeLabel(description);
    return normalizedDescription === 'course-internal posts, study organization, and student-life notices.' ||
      normalizedDescription === 'kursinterne beiträge, lernorganisation und hinweise für deinen studienalltag.';
  }
}
