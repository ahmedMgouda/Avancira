import { TestBed } from '@angular/core/testing';

import { GridStateService } from './grid-state.service.service';

describe('GridStateService', () => {
  let service: GridStateService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(GridStateService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
