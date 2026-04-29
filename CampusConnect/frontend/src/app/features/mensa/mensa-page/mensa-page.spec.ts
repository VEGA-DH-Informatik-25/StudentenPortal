import { registerLocaleData } from '@angular/common';
import localeDe from '@angular/common/locales/de';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { Mensa } from '../../../core/services/mensa';
import { MensaPage } from './mensa-page';

registerLocaleData(localeDe);

describe('MensaPage', () => {
  let component: MensaPage;
  let fixture: ComponentFixture<MensaPage>;
  const mensaService: Pick<Mensa, 'getWeekMenu'> = {
    getWeekMenu: () => of([
      {
        date: '2026-04-28',
        dishes: [
          {
            name: 'Kartoffeltaschen mit Frischkäsefüllung',
            nameLines: ['Kartoffeltaschen mit Frischkäsefüllung'],
            category: 'Essen 1',
            priceStudent: 3.2,
            allergens: 'Milch',
            isVegetarian: true,
            isVegan: false,
          },
        ],
      },
    ]),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MensaPage],
      providers: [{ provide: Mensa, useValue: mensaService }],
    }).compileComponents();

    fixture = TestBed.createComponent(MensaPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should ignore day selections outside the loaded menu', () => {
    component['selectDay'](99);

    expect(component['_selectedDay']()).toBe(0);

    component['selectDay'](-1);

    expect(component['_selectedDay']()).toBe(0);
  });

  it('should derive readable category markers', () => {
    expect(component['categoryMarker']('Essen 1')).toBe('E1');
    expect(component['categoryMarker']('  ')).toBe('ME');
  });

  it('should fall back to the dish name when no pre-split name lines exist', () => {
    expect(component['dishNameLines']({
      name: 'Pasta mit Tomatensauce',
      nameLines: [],
      category: 'Essen 2',
      priceStudent: 3.4,
      allergens: null,
      isVegetarian: true,
      isVegan: false,
    })).toEqual(['Pasta mit Tomatensauce']);
  });
});
