import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TimetableResponse } from '../models/timetable.model';

const SELECTED_COURSE_KEY = 'campusconnect.timetable.selectedCourse';
const COURSE_HISTORY_KEY = 'campusconnect.timetable.courseHistory';

@Injectable({ providedIn: 'root' })
export class Timetable {
  private readonly _http = inject(HttpClient);

  private readonly _defaultCourses = [
    'TIF24A',
    'TIF24B',
    'TIF25A',
    'TIF25B',
    'WWI24A',
    'WWI24B',
    'WWI25A',
    'WWI25B',
    'WDS24A',
    'WDS25A',
  ];

  getTimetable(course: string, days = 30): Observable<TimetableResponse> {
    const params = new HttpParams()
      .set('course', this.normalizeCourse(course))
      .set('days', days);

    return this._http.get<TimetableResponse>('/api/timetable', { params });
  }

  getCourseOptions(): string[] {
    return [...new Set([...this.getCourseHistory(), ...this._defaultCourses])].sort((a, b) =>
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