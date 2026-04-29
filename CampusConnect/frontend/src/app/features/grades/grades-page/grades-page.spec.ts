import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { Grade, GradePlan, GradeSummary } from '../../../core/models/grade.model';
import { Grades } from '../../../core/services/grades';
import { GradesPage } from './grades-page';

describe('GradesPage', () => {
  let component: GradesPage;
  let fixture: ComponentFixture<GradesPage>;
  let summary: GradeSummary;
  let plan: GradePlan;
  let gradesService: {
    getGrades: ReturnType<typeof vi.fn>;
    getGradePlan: ReturnType<typeof vi.fn>;
    addGrade: ReturnType<typeof vi.fn>;
    deleteGrade: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    summary = { grades: [], weightedAverage: 0, totalEcts: 0 };
    plan = {
      courseCode: 'TIF25A',
      studyProgram: 'Informatik',
      sourceUrl: 'https://example.invalid/Informatik.pdf',
      retrievedAt: '2026-04-29T10:00:00Z',
      modules: [
        { code: 'T4INF1001', name: 'Mathematik I', studyYear: 1, ects: 10, isRequired: true, isCompleted: false, grade: null, exams: [{ name: 'Klausur', scope: 'Siehe Pruefungsordnung', isGraded: true }] },
        { code: 'T4INF1004', name: 'Programmieren', studyYear: 1, ects: 5, isRequired: true, isCompleted: false, grade: null, exams: [] },
      ],
    };
    gradesService = {
      getGrades: vi.fn(() => of(summary)),
      getGradePlan: vi.fn(() => of(plan)),
      addGrade: vi.fn((request: { moduleName?: string | null; moduleCode?: string | null; value: number; ects?: number | null }) => {
        const plannedModule = plan.modules.find(module => module.code === request.moduleCode);
        const grade: Grade = {
          id: `grade-${summary.grades.length + 1}`,
          moduleCode: plannedModule?.code ?? null,
          moduleName: plannedModule?.name ?? request.moduleName ?? '',
          value: request.value,
          ects: plannedModule?.ects ?? request.ects ?? 0,
          createdAt: '2026-04-28T10:00:00Z',
        };
        summary = createSummary([...summary.grades, grade]);
        plan = {
          ...plan,
          modules: plan.modules.map(module => module.code === grade.moduleCode
            ? { ...module, isCompleted: true, grade: grade.value }
            : module),
        };
        return of(grade);
      }),
      deleteGrade: vi.fn((id: string) => {
        const deleted = summary.grades.find(grade => grade.id === id);
        summary = createSummary(summary.grades.filter(grade => grade.id !== id));
        plan = {
          ...plan,
          modules: plan.modules.map(module => module.code === deleted?.moduleCode
            ? { ...module, isCompleted: false, grade: null }
            : module),
        };
        return of(undefined);
      }),
    };

    await TestBed.configureTestingModule({
      imports: [GradesPage],
      providers: [{ provide: Grades, useValue: gradesService }],
    }).compileComponents();

    fixture = TestBed.createComponent(GradesPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load grades from the backend service', () => {
    expect(gradesService.getGrades).toHaveBeenCalled();
    expect(gradesService.getGradePlan).toHaveBeenCalled();
    expect(component['grades']()).toEqual([]);
  });

  it('should add grades through the backend service and calculate weighted averages', () => {
    component['selectedModuleCode'] = 'T4INF1001';
    component['grade'] = 2;
    component['addGrade']();

    component['selectedModuleCode'] = 'T4INF1004';
    component['grade'] = 1;
    component['addGrade']();

    expect(gradesService.addGrade).toHaveBeenCalledWith({ moduleCode: 'T4INF1001', value: 2 });
    expect(component['grades']().length).toBe(2);
    expect(component['weightedAverage']()).toBeCloseTo(1.67, 1);
    expect(component['passedCredits']()).toBe(15);
    expect(component['moduleSummaries']().length).toBeGreaterThan(0);
  });

  it('should preview additional grades without saving them', () => {
    component['selectedModuleCode'] = 'T4INF1001';
    component['grade'] = 3;
    component['addGrade']();
    component['simulationGrade'] = 1;
    component['simulationEcts'] = 5;

    expect(component['simulatedAverage']()).toBeCloseTo(2.33, 1);
    expect(component['grades']().length).toBe(1);
  });

  it('should delete grades through the backend service', () => {
    component['selectedModuleCode'] = 'T4INF1001';
    component['addGrade']();

    component['removeGrade']('grade-1');

    expect(gradesService.deleteGrade).toHaveBeenCalledWith('grade-1');
    expect(component['grades']()).toEqual([]);
  });
});

function createSummary(grades: Grade[]): GradeSummary {
  const totalEcts = grades.reduce((sum, grade) => sum + grade.ects, 0);
  const weightedAverage = totalEcts === 0
    ? 0
    : Math.round((grades.reduce((sum, grade) => sum + grade.value * grade.ects, 0) / totalEcts) * 100) / 100;

  return { grades, weightedAverage, totalEcts };
}
