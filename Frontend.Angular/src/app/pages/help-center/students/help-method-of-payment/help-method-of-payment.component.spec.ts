import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpMethodOfPaymentComponent } from './help-method-of-payment.component';

describe('HelpMethodOfPaymentComponent', () => {
  let component: HelpMethodOfPaymentComponent;
  let fixture: ComponentFixture<HelpMethodOfPaymentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpMethodOfPaymentComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpMethodOfPaymentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
