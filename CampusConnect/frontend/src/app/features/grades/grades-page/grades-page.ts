import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Grade, GradePlan, GradePlanModule, GradeSummary } from '../../../core/models/grade.model';
import { Grades } from '../../../core/services/grades';

interface ModuleSummary {
  module: string;
  average: number;
  credits: number;
  count: number;
}

@Component({
  selector: 'app-grades-page',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './grades-page.html',
  styleUrl: './grades-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GradesPage implements OnInit {
  private readonly _gradesService = inject(Grades);

  protected readonly _summary = signal<GradeSummary>({ grades: [], weightedAverage: 0, totalEcts: 0 });
  protected readonly _plan = signal<GradePlan | null>(null);
  protected readonly _isLoading = signal(false);
  protected readonly _isPlanLoading = signal(false);
  protected readonly _isSubmitting = signal(false);
  protected readonly _error = signal<string | null>(null);
  protected readonly _planNotice = signal<string | null>(null);

  protected selectedModuleCode = '';
  protected moduleName = '';
  protected grade = 2.0;
  protected ects = 5;
  protected simulationGrade = 2.0;
  protected simulationEcts = 5;
  protected targetAverage = 2.5;

  protected readonly grades = computed(() => this._summary().grades);
  protected readonly planModules = computed(() => this._plan()?.modules ?? []);
  protected readonly openPlanModules = computed(() => this.planModules().filter(module => !module.isCompleted));
  protected readonly completedPlanModules = computed(() => this.planModules().filter(module => module.isCompleted).length);
  protected readonly totalEcts = computed(() => this._summary().totalEcts);
  protected readonly weightedAverage = computed(() => this.grades().length === 0 ? Number.NaN : this._summary().weightedAverage);
  protected readonly passedCredits = computed(() =>
    this.grades()
      .filter(entry => entry.value <= 4)
      .reduce((sum, entry) => sum + entry.ects, 0),
  );
  protected readonly failedCount = computed(() => this.grades().filter(entry => entry.value > 4).length);
  protected readonly bestGrade = computed(() => this.bestGradeValue());
  protected readonly simulatedAverage = computed(() =>
    this.average([
      ...this.grades(),
      {
        id: 'simulation',
        moduleName: 'Simulation',
        value: this.normalizeGrade(this.simulationGrade),
        ects: this.normalizeEcts(this.simulationEcts),
        createdAt: '',
      },
    ]),
  );
  protected readonly requiredGradeForTarget = computed(() => {
    const ects = this.normalizeEcts(this.simulationEcts);
    const currentEcts = this.totalEcts();

    if (currentEcts === 0) {
      return this.targetAverage;
    }

    return (this.targetAverage * (currentEcts + ects) - this.weightedSum(this.grades())) / ects;
  });
  protected readonly moduleSummaries = computed<ModuleSummary[]>(() => {
    const modules = new Map<string, Grade[]>();

    for (const entry of this.grades()) {
      let entries = modules.get(entry.moduleName);

      if (!entries) {
        entries = [];
        modules.set(entry.moduleName, entries);
      }

      entries.push(entry);
    }

    return [...modules.entries()]
      .map(([module, entries]) => ({
        module,
        average: this.average(entries),
        credits: entries.filter(entry => entry.value <= 4).reduce((sum, entry) => sum + entry.ects, 0),
        count: entries.length,
      }))
      .sort((a, b) => a.module.localeCompare(b.module));
  });

  ngOnInit(): void {
    this._loadGrades();
    this._loadPlan();
  }

  protected addGrade(form?: NgForm): void {
    const selectedModule = this.selectedPlanModule();
    const hasPlan = this.planModules().length > 0;
    const moduleName = this.moduleName.trim();

    if (hasPlan && !selectedModule) {
      this._error.set('Bitte wähle ein Modul aus deinem Kursplan aus.');
      return;
    }

    if (!hasPlan && !moduleName) {
      this._error.set('Bitte gib ein Modul oder eine Prüfung ein.');
      return;
    }

    this._isSubmitting.set(true);
    this._error.set(null);

    this._gradesService.addGrade(selectedModule
      ? {
          moduleCode: selectedModule.code,
          value: this.normalizeGrade(this.grade),
        }
      : {
          moduleName,
          value: this.normalizeGrade(this.grade),
          ects: this.normalizeEcts(this.ects),
        }).subscribe({
      next: () => {
        this._isSubmitting.set(false);
        form?.resetForm({
          selectedModuleCode: this.nextOpenModuleCode(),
          moduleName: '',
          grade: 2.0,
          ects: 5,
        });
        this.selectedModuleCode = this.nextOpenModuleCode();
        this.moduleName = '';
        this.grade = 2.0;
        this.ects = 5;
        this._loadGrades();
        this._loadPlan();
      },
      error: error => {
        this._error.set(this._readError(error, 'Note konnte nicht gespeichert werden.'));
        this._isSubmitting.set(false);
      },
    });
  }

  protected removeGrade(id: string): void {
    this._gradesService.deleteGrade(id).subscribe({
      next: () => {
        this._summary.set(this.createSummary(this.grades().filter(entry => entry.id !== id)));
        this._loadPlan();
      },
      error: error => this._error.set(this._readError(error, 'Note konnte nicht gelöscht werden.')),
    });
  }

  protected selectedPlanModule(): GradePlanModule | null {
    return this.planModules().find(module => module.code === this.selectedModuleCode) ?? null;
  }

  protected examText(module: GradePlanModule): string {
    if (module.exams.length === 0) {
      return 'Prüfungsform laut DHBW-Plan noch nicht eindeutig angegeben';
    }

    return module.exams
      .map(exam => [exam.name, exam.scope].filter(Boolean).join(' · '))
      .join(', ');
  }

  protected format(value: number): string {
    if (!Number.isFinite(value)) {
      return '–';
    }

    return value.toLocaleString('de-DE', { minimumFractionDigits: 1, maximumFractionDigits: 2 });
  }

  protected targetHint(): string {
    const required = this.requiredGradeForTarget();

    if (!Number.isFinite(required)) {
      return 'Lege zuerst bestehende Noten oder eine Gewichtung fest.';
    }

    if (required < 1) {
      return 'Das Ziel ist bereits sicher erreichbar.';
    }

    if (required > 5) {
      return 'Das Ziel ist mit dieser Gewichtung nicht mehr erreichbar.';
    }

    return `Benötigte Zusatznote: ${this.format(required)}`;
  }

  private _loadGrades(): void {
    this._isLoading.set(true);
    this._error.set(null);

    this._gradesService.getGrades().subscribe({
      next: summary => {
        this._summary.set(summary);
        this._isLoading.set(false);
      },
      error: error => {
        this._error.set(this._readError(error, 'Noten konnten nicht geladen werden.'));
        this._isLoading.set(false);
      },
    });
  }

  private _loadPlan(): void {
    this._isPlanLoading.set(true);
    this._planNotice.set(null);

    this._gradesService.getGradePlan().subscribe({
      next: plan => {
        this._plan.set(plan);
        if (!this.selectedModuleCode || !this.planModules().some(module => module.code === this.selectedModuleCode && !module.isCompleted)) {
          this.selectedModuleCode = this.nextOpenModuleCode();
        }
        this._isPlanLoading.set(false);
      },
      error: error => {
        this._plan.set(null);
        this._planNotice.set(this._readError(error, 'Für deinen Kurs wurde kein DHBW-Studienplan gefunden.'));
        this.selectedModuleCode = '';
        this._isPlanLoading.set(false);
      },
    });
  }

  private average(entries: Grade[]): number {
    const ects = this.sumEcts(entries);

    return ects === 0 ? Number.NaN : this.weightedSum(entries) / ects;
  }

  private weightedSum(entries: Grade[]): number {
    return entries.reduce((sum, entry) => sum + entry.value * entry.ects, 0);
  }

  private sumEcts(entries: Grade[]): number {
    return entries.reduce((sum, entry) => sum + entry.ects, 0);
  }

  private bestGradeValue(): number {
    const values = this.grades().map(entry => entry.value);

    return values.length === 0 ? Number.NaN : Math.min(...values);
  }

  private normalizeGrade(value: number): number {
    const parsed = Number(value);

    return Number.isFinite(parsed) ? Math.min(5, Math.max(1, parsed)) : 1;
  }

  private normalizeEcts(value: number): number {
    const parsed = Number(value);

    return Number.isFinite(parsed) ? Math.max(1, Math.round(parsed)) : 1;
  }

  private createSummary(grades: Grade[]): GradeSummary {
    const totalEcts = this.sumEcts(grades);
    const weightedAverage = totalEcts === 0 ? 0 : Math.round((this.weightedSum(grades) / totalEcts) * 100) / 100;
    return { grades, weightedAverage, totalEcts };
  }

  private nextOpenModuleCode(): string {
    return this.openPlanModules()[0]?.code ?? this.planModules()[0]?.code ?? '';
  }

  private _readError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as { error?: string } | null;
      return body?.error ?? fallback;
    }

    return fallback;
  }
}
