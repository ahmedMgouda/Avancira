import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpLeaveOrRequestAReviewComponent } from './help-leave-or-request-a-review.component';

describe('HelpLeaveOrRequestAReviewComponent', () => {
  let component: HelpLeaveOrRequestAReviewComponent;
  let fixture: ComponentFixture<HelpLeaveOrRequestAReviewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpLeaveOrRequestAReviewComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpLeaveOrRequestAReviewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
