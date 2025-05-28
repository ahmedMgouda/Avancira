import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpLessonPackPaymentTimelinesComponent } from './help-lesson-pack-payment-timelines.component';

describe('HelpLessonPackPaymentTimelinesComponent', () => {
  let component: HelpLessonPackPaymentTimelinesComponent;
  let fixture: ComponentFixture<HelpLessonPackPaymentTimelinesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpLessonPackPaymentTimelinesComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpLessonPackPaymentTimelinesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
