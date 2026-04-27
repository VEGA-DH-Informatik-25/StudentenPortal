import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Grades } from '../../../core/services/grades';
import { GradeSummary } from '../../../core/models/grade.model';

@Component({
  selector: 'app-grades-page',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './grades-page.html',
  styleUrl: './grades-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GradesPage implements OnInit {
  private readonly _gradesService = inject(Grades);

  protected readonly _summary = signal<GradeSummary | null>(null);
  protected readonly _isLoading = signal(false);
  protected readonly _showForm = signal(false);
  protected readonly _form = { moduleName: '', value: 1.0, ects: 5 };

  ngOnInit(): void {
    this._loadGrades();
  }

  private _loadGrades(): void {
    this._isLoading.set(true);
    this._gradesService.getGrades().subscribe({
      next: s => { this._summary.set(s); this._isLoading.set(false); },
      error: () => this._isLoading.set(false),
    });
  }

  protected onAdd(): void {
    this._gradesService.addGrade(this._form).subscribe({
      next: () => { this._showForm.set(false); this._loadGrades(); },
    });
  }
}

