import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Course } from '../models/course.model';

@Injectable({ providedIn: 'root' })
export class Courses {
  private readonly _http = inject(HttpClient);

  getCourses(): Observable<Course[]> {
    return this._http.get<Course[]>('/api/courses');
  }
}
