import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpRefundComponent } from './help-refund.component';

describe('HelpRefundComponent', () => {
  let component: HelpRefundComponent;
  let fixture: ComponentFixture<HelpRefundComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpRefundComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpRefundComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
