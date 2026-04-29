import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Grade, GradePlan, GradeSummary, AddGradeRequest } from '../models/grade.model';

@Injectable({ providedIn: 'root' })
export class Grades {
  private readonly _http = inject(HttpClient);

  getGrades(): Observable<GradeSummary> {
    return this._http.get<GradeSummary>('/api/grades');
  }

  getGradePlan(): Observable<GradePlan> {
    return this._http.get<GradePlan>('/api/grades/plan');
  }

  addGrade(req: AddGradeRequest): Observable<Grade> {
    return this._http.post<Grade>('/api/grades', req);
  }

  deleteGrade(id: string): Observable<void> {
    return this._http.delete<void>(`/api/grades/${id}`);
  }
}
