import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { Grade, GradeSummary } from '../../../core/models/grade.model';
import { Grades } from '../../../core/services/grades';
import { GradesPage } from './grades-page';

describe('GradesPage', () => {
  let component: GradesPage;
  let fixture: ComponentFixture<GradesPage>;
  let summary: GradeSummary;
  let gradesService: {
    getGrades: ReturnType<typeof vi.fn>;
    addGrade: ReturnType<typeof vi.fn>;
    deleteGrade: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    summary = { grades: [], weightedAverage: 0, totalEcts: 0 };
    gradesService = {
      getGrades: vi.fn(() => of(summary)),
      addGrade: vi.fn((request: { moduleName: string; value: number; ects: number }) => {
        const grade: Grade = {
          id: `grade-${summary.grades.length + 1}`,
          moduleName: request.moduleName,
          value: request.value,
          ects: request.ects,
          createdAt: '2026-04-28T10:00:00Z',
        };
        summary = createSummary([...summary.grades, grade]);
        return of(grade);
      }),
      deleteGrade: vi.fn((id: string) => {
        summary = createSummary(summary.grades.filter(grade => grade.id !== id));
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
    expect(component['grades']()).toEqual([]);
  });

  it('should add grades through the backend service and calculate weighted averages', () => {
    component['moduleName'] = 'Mathematik';
    component['grade'] = 2;
    component['ects'] = 10;
    component['addGrade']();

    component['moduleName'] = 'Programmieren';
    component['grade'] = 1;
    component['ects'] = 5;
    component['addGrade']();

    expect(gradesService.addGrade).toHaveBeenCalledWith({ moduleName: 'Mathematik', value: 2, ects: 10 });
    expect(component['grades']().length).toBe(2);
    expect(component['weightedAverage']()).toBeCloseTo(1.67, 1);
    expect(component['passedCredits']()).toBe(15);
    expect(component['moduleSummaries']().length).toBeGreaterThan(0);
  });

  it('should preview additional grades without saving them', () => {
    component['moduleName'] = 'Mathematik';
    component['grade'] = 3;
    component['ects'] = 5;
    component['addGrade']();
    component['simulationGrade'] = 1;
    component['simulationEcts'] = 5;

    expect(component['simulatedAverage']()).toBe(2);
    expect(component['grades']().length).toBe(1);
  });

  it('should delete grades through the backend service', () => {
    component['moduleName'] = 'Mathematik';
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
