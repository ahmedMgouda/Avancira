import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpChangeMyPaymentMethodComponent } from './help-change-my-payment-method.component';

describe('HelpChangeMyPaymentMethodComponent', () => {
  let component: HelpChangeMyPaymentMethodComponent;
  let fixture: ComponentFixture<HelpChangeMyPaymentMethodComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpChangeMyPaymentMethodComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpChangeMyPaymentMethodComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
