import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type AssessmentType = 'Klausur' | 'Projekt' | 'Mündlich' | 'Hausarbeit';

interface GradeEntry {
  id: number;
  subject: string;
  module: string;
  type: AssessmentType;
  grade: number;
  weight: number;
  credits: number;
}

interface ModuleSummary {
  module: string;
  average: number;
  credits: number;
  count: number;
}

@Component({
  selector: 'app-grades-page',
  imports: [FormsModule],
  templateUrl: './grades-page.html',
  styleUrl: './grades-page.scss',
})
export class GradesPage {
  private readonly storageKey = 'campusconnect.grades';

  protected readonly types: AssessmentType[] = ['Klausur', 'Projekt', 'Mündlich', 'Hausarbeit'];
  protected readonly grades = signal<GradeEntry[]>(this.readGrades());

  protected subject = '';
  protected module = '';
  protected type: AssessmentType = 'Klausur';
  protected grade = 2.0;
  protected weight = 1;
  protected credits = 5;
  protected simulationGrade = 2.0;
  protected simulationWeight = 1;
  protected targetAverage = 2.5;

  protected readonly totalWeight = computed(() => this.sumWeight(this.grades()));
  protected readonly weightedAverage = computed(() => this.average(this.grades()));
  protected readonly passedCredits = computed(() =>
    this.grades()
      .filter(entry => entry.grade <= 4)
      .reduce((sum, entry) => sum + entry.credits, 0),
  );
  protected readonly failedCount = computed(() => this.grades().filter(entry => entry.grade > 4).length);
  protected readonly bestGrade = computed(() => this.extremeGrade('best'));
  protected readonly simulatedAverage = computed(() =>
    this.average([
      ...this.grades(),
      {
        id: 0,
        subject: 'Simulation',
        module: 'Simulation',
        type: 'Klausur',
        grade: this.normalizeGrade(this.simulationGrade),
        weight: this.normalizePositive(this.simulationWeight, 1),
        credits: 0,
      },
    ]),
  );
  protected readonly requiredGradeForTarget = computed(() => {
    const weight = this.normalizePositive(this.simulationWeight, 1);
    const currentWeight = this.totalWeight();

    if (currentWeight === 0) {
      return this.targetAverage;
    }

    return (this.targetAverage * (currentWeight + weight) - this.weightedSum(this.grades())) / weight;
  });
  protected readonly moduleSummaries = computed<ModuleSummary[]>(() => {
    const modules = new Map<string, GradeEntry[]>();

    for (const entry of this.grades()) {
      modules.set(entry.module, [...(modules.get(entry.module) ?? []), entry]);
    }

    return [...modules.entries()]
      .map(([module, entries]) => ({
        module,
        average: this.average(entries),
        credits: entries.filter(entry => entry.grade <= 4).reduce((sum, entry) => sum + entry.credits, 0),
        count: entries.length,
      }))
      .sort((a, b) => a.module.localeCompare(b.module));
  });

  protected addGrade(): void {
    const subject = this.subject.trim();

    if (!subject) {
      return;
    }

    const entry: GradeEntry = {
      id: Date.now(),
      subject,
      module: this.module.trim() || subject,
      type: this.type,
      grade: this.normalizeGrade(this.grade),
      weight: this.normalizePositive(this.weight, 1),
      credits: this.normalizePositive(this.credits, 0),
    };

    this.updateGrades([...this.grades(), entry]);
    this.subject = '';
    this.module = '';
    this.grade = 2.0;
    this.weight = 1;
    this.credits = 5;
  }

  protected removeGrade(id: number): void {
    this.updateGrades(this.grades().filter(entry => entry.id !== id));
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

  private updateGrades(grades: GradeEntry[]): void {
    this.grades.set(grades);
    localStorage.setItem(this.storageKey, JSON.stringify(grades));
  }

  private readGrades(): GradeEntry[] {
    const raw = localStorage.getItem(this.storageKey);

    if (!raw) {
      return [];
    }

    try {
      const parsed = JSON.parse(raw) as GradeEntry[];
      return Array.isArray(parsed) ? parsed.filter(entry => this.isGradeEntry(entry)) : [];
    } catch {
      return [];
    }
  }

  private isGradeEntry(entry: GradeEntry): entry is GradeEntry {
    return (
      typeof entry.id === 'number' &&
      typeof entry.subject === 'string' &&
      typeof entry.module === 'string' &&
      this.types.includes(entry.type) &&
      typeof entry.grade === 'number' &&
      typeof entry.weight === 'number' &&
      typeof entry.credits === 'number'
    );
  }

  private average(entries: GradeEntry[]): number {
    const weight = this.sumWeight(entries);

    return weight === 0 ? Number.NaN : this.weightedSum(entries) / weight;
  }

  private weightedSum(entries: GradeEntry[]): number {
    return entries.reduce((sum, entry) => sum + entry.grade * entry.weight, 0);
  }

  private sumWeight(entries: GradeEntry[]): number {
    return entries.reduce((sum, entry) => sum + entry.weight, 0);
  }

  private extremeGrade(kind: 'best'): number {
    const values = this.grades().map(entry => entry.grade);

    return values.length === 0 ? Number.NaN : Math.min(...values);
  }

  private normalizeGrade(value: number): number {
    return Math.min(5, Math.max(1, Number(value) || 1));
  }

  private normalizePositive(value: number, fallback: number): number {
    return Math.max(0, Number(value) || fallback);
  }
}
