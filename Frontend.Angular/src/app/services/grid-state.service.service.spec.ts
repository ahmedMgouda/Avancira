import { TestBed } from '@angular/core/testing';

import { GridStateServiceService } from './grid-state.service.service';

describe('GridStateServiceService', () => {
  let service: GridStateServiceService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(GridStateServiceService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
