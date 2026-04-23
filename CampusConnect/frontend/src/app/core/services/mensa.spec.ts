import { TestBed } from '@angular/core/testing';

import { Mensa } from './mensa';

describe('Mensa', () => {
  let service: Mensa;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Mensa);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
