import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpInvoicesComponent } from './help-invoices.component';

describe('HelpInvoicesComponent', () => {
  let component: HelpInvoicesComponent;
  let fixture: ComponentFixture<HelpInvoicesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpInvoicesComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpInvoicesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
