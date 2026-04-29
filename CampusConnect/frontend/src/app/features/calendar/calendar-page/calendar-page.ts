import { HttpErrorResponse } from '@angular/common/http';
import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Calendar } from '../../../core/services/calendar';
import { ExamEntry } from '../../../core/models/exam.model';

@Component({
  selector: 'app-calendar-page',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './calendar-page.html',
  styleUrl: './calendar-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarPage implements OnInit {
  private readonly _calendarService = inject(Calendar);

  protected readonly _exams = signal<ExamEntry[]>([]);
  protected readonly _showForm = signal(false);
  protected readonly _isSubmitting = signal(false);
  protected readonly _error = signal<string | null>(null);
  protected readonly _form = { moduleName: '', examDate: '', location: '', notes: '' };

  ngOnInit(): void {
    this._loadExams();
  }

  private _loadExams(): void {
    this._calendarService.getExams().subscribe({
      next: exams => {
        this._exams.set(exams);
        this._error.set(null);
      },
      error: error => this._error.set(this._readError(error, 'Prüfungen konnten nicht geladen werden.')),
    });
  }

  protected onAdd(): void {
    const moduleName = this._form.moduleName.trim();
    if (!moduleName || !this._form.examDate) {
      this._error.set('Bitte gib Modul und Datum an.');
      return;
    }

    const examDate = new Date(this._form.examDate);
    if (Number.isNaN(examDate.getTime())) {
      this._error.set('Bitte gib ein gültiges Prüfungsdatum an.');
      return;
    }

    this._isSubmitting.set(true);
    this._error.set(null);
    this._calendarService.addExam({
      moduleName,
      examDate: examDate.toISOString(),
      location: this._form.location.trim() || undefined,
      notes: this._form.notes.trim() || undefined,
    }).subscribe({
      next: () => {
        this._showForm.set(false);
        this._isSubmitting.set(false);
        this._form.moduleName = '';
        this._form.examDate = '';
        this._form.location = '';
        this._form.notes = '';
        this._loadExams();
      },
      error: error => {
        this._error.set(this._readError(error, 'Prüfung konnte nicht gespeichert werden.'));
        this._isSubmitting.set(false);
      },
    });
  }

  protected onDelete(id: string): void {
    this._error.set(null);
    this._calendarService.deleteExam(id).subscribe({
      next: () => this._exams.update(e => e.filter(x => x.id !== id)),
      error: error => this._error.set(this._readError(error, 'Prüfung konnte nicht gelöscht werden.')),
    });
  }

  private _readError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as { error?: string } | null;
      return body?.error ?? fallback;
    }

    return fallback;
  }
}

