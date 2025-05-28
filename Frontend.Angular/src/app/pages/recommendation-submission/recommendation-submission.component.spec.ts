import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RecommendationSubmissionComponent } from './recommendation-submission.component';

describe('RecommendationSubmissionComponent', () => {
  let component: RecommendationSubmissionComponent;
  let fixture: ComponentFixture<RecommendationSubmissionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecommendationSubmissionComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RecommendationSubmissionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
