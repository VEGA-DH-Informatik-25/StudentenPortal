import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TimetableResponse } from '../models/timetable.model';

const SELECTED_COURSE_KEY = 'campusconnect.timetable.selectedCourse';
const COURSE_HISTORY_KEY = 'campusconnect.timetable.courseHistory';
const DEFAULT_TIMETABLE_LOOKAHEAD_DAYS = 120;

@Injectable({ providedIn: 'root' })
export class Timetable {
  private readonly _http = inject(HttpClient);

  getTimetable(course: string, days = DEFAULT_TIMETABLE_LOOKAHEAD_DAYS): Observable<TimetableResponse> {
    const params = new HttpParams()
      .set('course', this.normalizeCourse(course))
      .set('days', days);

    return this._http.get<TimetableResponse>('/api/timetable', { params });
  }

  getCourseOptions(courseCodes: string[] = []): string[] {
    const normalizedCourseCodes = courseCodes.map(course => this.normalizeCourse(course)).filter(Boolean);
    return [...new Set([...this.getCourseHistory(), ...normalizedCourseCodes])].sort((a, b) =>
      a.localeCompare(b, 'de', { numeric: true, sensitivity: 'base' })
    );
  }

  getStoredCourse(): string {
    return localStorage.getItem(SELECTED_COURSE_KEY) ?? '';
  }

  storeCourse(course: string): void {
    const normalizedCourse = this.normalizeCourse(course);
    if (!normalizedCourse) {
      return;
    }

    localStorage.setItem(SELECTED_COURSE_KEY, normalizedCourse);
    const history = this.getCourseHistory().filter(item => item !== normalizedCourse);
    localStorage.setItem(COURSE_HISTORY_KEY, JSON.stringify([normalizedCourse, ...history].slice(0, 6)));
  }

  normalizeCourse(course: string): string {
    return course.trim().toUpperCase();
  }

  private getCourseHistory(): string[] {
    const raw = localStorage.getItem(COURSE_HISTORY_KEY);
    if (!raw) {
      return [];
    }

    try {
      const parsed: unknown = JSON.parse(raw);
      if (!Array.isArray(parsed)) {
        return [];
      }

      return parsed
        .filter((value): value is string => typeof value === 'string')
        .map(value => this.normalizeCourse(value))
        .filter(Boolean);
    } catch {
      return [];
    }
  }
}
