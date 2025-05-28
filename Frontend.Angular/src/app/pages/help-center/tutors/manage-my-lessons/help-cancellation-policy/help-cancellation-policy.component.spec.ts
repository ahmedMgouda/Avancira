import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpCancellationPolicyComponent } from './help-cancellation-policy.component';

describe('HelpCancellationPolicyComponent', () => {
  let component: HelpCancellationPolicyComponent;
  let fixture: ComponentFixture<HelpCancellationPolicyComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpCancellationPolicyComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpCancellationPolicyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
