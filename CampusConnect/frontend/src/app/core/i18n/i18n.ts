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

@Injectable({ providedIn: 'root' })
export class I18n {
  readonly languages: LanguageOption[] = [
    { code: 'en', label: 'English', shortLabel: 'EN', locale: 'en-US' },
    { code: 'de', label: 'Deutsch', shortLabel: 'DE', locale: 'de-DE' },
  ];

  private readonly _language = signal<LanguageCode>(this._initialLanguage());
  readonly language = this._language.asReadonly();

  setLanguage(language: string): void {
    if (!this._isLanguageCode(language)) {
      return;
    }

    this._language.set(language);
    globalThis.localStorage?.setItem(STORAGE_KEY, language);
  }

  locale(): string {
    return this.languages.find(language => language.code === this._language())?.locale ?? 'en-US';
  }

  translate(key: TranslationKey, params: Record<string, string | number> = {}): string {
    const template = translations[this._language()][key] ?? translations.en[key] ?? key;
    return template.replace(/{{\s*(\w+)\s*}}/g, (_, token: string) => String(params[token] ?? ''));
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
    if (storedLanguage && this._isLanguageCode(storedLanguage)) {
      return storedLanguage;
    }

    return globalThis.navigator?.language?.toLowerCase().startsWith('de') ? 'de' : 'en';
  }

  private _isLanguageCode(language: string): language is LanguageCode {
    return language === 'en' || language === 'de';
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