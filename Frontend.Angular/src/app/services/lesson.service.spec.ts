import { TestBed } from '@angular/core/testing';

import { LessonService } from './lesson.service';

describe('PropositionService', () => {
  let service: LessonService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LessonService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
