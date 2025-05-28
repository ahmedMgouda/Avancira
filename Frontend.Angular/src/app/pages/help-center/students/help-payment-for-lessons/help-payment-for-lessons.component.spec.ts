import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpPaymentForLessonsComponent } from './help-payment-for-lessons.component';

describe('HelpPaymentForLessonsComponent', () => {
  let component: HelpPaymentForLessonsComponent;
  let fixture: ComponentFixture<HelpPaymentForLessonsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpPaymentForLessonsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpPaymentForLessonsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
