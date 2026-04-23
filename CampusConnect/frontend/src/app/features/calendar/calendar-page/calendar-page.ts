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
  protected readonly _form = { moduleName: '', examDate: '', location: '', notes: '' };

  ngOnInit(): void {
    this._loadExams();
  }

  private _loadExams(): void {
    this._calendarService.getExams().subscribe({
      next: exams => this._exams.set(exams),
    });
  }

  protected onAdd(): void {
    this._calendarService.addExam({
      moduleName: this._form.moduleName,
      examDate: new Date(this._form.examDate).toISOString(),
      location: this._form.location || undefined,
      notes: this._form.notes || undefined,
    }).subscribe({
      next: () => { this._showForm.set(false); this._loadExams(); },
    });
  }

  protected onDelete(id: string): void {
    this._calendarService.deleteExam(id).subscribe({
      next: () => this._exams.update(e => e.filter(x => x.id !== id)),
    });
  }
}

