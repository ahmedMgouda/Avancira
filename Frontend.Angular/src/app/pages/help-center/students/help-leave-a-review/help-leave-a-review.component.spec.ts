import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpLeaveAReviewComponent } from './help-leave-a-review.component';

describe('HelpLeaveAReviewComponent', () => {
  let component: HelpLeaveAReviewComponent;
  let fixture: ComponentFixture<HelpLeaveAReviewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpLeaveAReviewComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpLeaveAReviewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
