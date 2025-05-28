import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PaymentAltOptionsComponent } from './payment-alt-options.component';

describe('PaymentAltOptionsComponent', () => {
  let component: PaymentAltOptionsComponent;
  let fixture: ComponentFixture<PaymentAltOptionsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PaymentAltOptionsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PaymentAltOptionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
