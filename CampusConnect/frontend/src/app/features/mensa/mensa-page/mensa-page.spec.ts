import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MensaPage } from './mensa-page';

describe('MensaPage', () => {
  let component: MensaPage;
  let fixture: ComponentFixture<MensaPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MensaPage],
    }).compileComponents();

    fixture = TestBed.createComponent(MensaPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
