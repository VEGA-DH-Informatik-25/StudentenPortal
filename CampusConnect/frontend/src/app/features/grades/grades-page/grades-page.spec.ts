import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GradesPage } from './grades-page';

describe('GradesPage', () => {
  let component: GradesPage;
  let fixture: ComponentFixture<GradesPage>;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [GradesPage],
    }).compileComponents();

    fixture = TestBed.createComponent(GradesPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should add grades and calculate weighted averages', () => {
    component['subject'] = 'Mathematik';
    component['module'] = 'Grundlagen';
    component['grade'] = 2;
    component['weight'] = 2;
    component['credits'] = 5;
    component['addGrade']();

    component['subject'] = 'Programmieren';
    component['module'] = 'Grundlagen';
    component['grade'] = 1;
    component['weight'] = 1;
    component['credits'] = 5;
    component['addGrade']();

    expect(component['grades']().length).toBe(2);
    expect(component['weightedAverage']()).toBeCloseTo(1.67, 1);
    expect(component['passedCredits']()).toBe(10);
    expect(component['moduleSummaries']()[0].average).toBeCloseTo(1.67, 1);
  });

  it('should preview additional grades without saving them', () => {
    component['subject'] = 'Mathematik';
    component['grade'] = 3;
    component['weight'] = 1;
    component['addGrade']();
    component['simulationGrade'] = 1;
    component['simulationWeight'] = 1;

    expect(component['simulatedAverage']()).toBe(2);
    expect(component['grades']().length).toBe(1);
  });
});
