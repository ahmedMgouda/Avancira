import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpPaymentReceiptsComponent } from './help-payment-receipts.component';

describe('HelpPaymentReceiptsComponent', () => {
  let component: HelpPaymentReceiptsComponent;
  let fixture: ComponentFixture<HelpPaymentReceiptsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpPaymentReceiptsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpPaymentReceiptsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
