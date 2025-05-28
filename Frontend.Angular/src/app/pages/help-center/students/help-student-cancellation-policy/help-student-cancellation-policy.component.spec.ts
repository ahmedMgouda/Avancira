import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpStudentCancellationPolicyComponent } from './help-student-cancellation-policy.component';

describe('HelpStudentCancellationPolicyComponent', () => {
  let component: HelpStudentCancellationPolicyComponent;
  let fixture: ComponentFixture<HelpStudentCancellationPolicyComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpStudentCancellationPolicyComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpStudentCancellationPolicyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
