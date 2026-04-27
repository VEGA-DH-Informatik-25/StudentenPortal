import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ExamEntry, AddExamRequest } from '../models/exam.model';

@Injectable({ providedIn: 'root' })
export class Calendar {
  private readonly _http = inject(HttpClient);

  getExams(): Observable<ExamEntry[]> {
    return this._http.get<ExamEntry[]>('/api/calendar');
  }

  addExam(req: AddExamRequest): Observable<ExamEntry> {
    return this._http.post<ExamEntry>('/api/calendar', req);
  }

  deleteExam(id: string): Observable<void> {
    return this._http.delete<void>(`/api/calendar/${id}`);
  }
}

