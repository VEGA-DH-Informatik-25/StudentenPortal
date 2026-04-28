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
});
