import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GradeSummary, AddGradeRequest } from '../models/grade.model';

@Injectable({ providedIn: 'root' })
export class Grades {
  private readonly _http = inject(HttpClient);

  getGrades(): Observable<GradeSummary> {
    return this._http.get<GradeSummary>('/api/grades');
  }

  addGrade(req: AddGradeRequest): Observable<GradeSummary> {
    return this._http.post<GradeSummary>('/api/grades', req);
  }
}
