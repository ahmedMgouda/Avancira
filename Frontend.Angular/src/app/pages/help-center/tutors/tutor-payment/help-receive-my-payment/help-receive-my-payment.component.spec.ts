import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpReceiveMyPaymentComponent } from './help-receive-my-payment.component';

describe('HelpReceiveMyPaymentComponent', () => {
  let component: HelpReceiveMyPaymentComponent;
  let fixture: ComponentFixture<HelpReceiveMyPaymentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpReceiveMyPaymentComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpReceiveMyPaymentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
