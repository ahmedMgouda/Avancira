import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpBenefitsOfTheStudentPassComponent } from './help-benefits-of-the-student-pass.component';

describe('HelpBenefitsOfTheStudentPassComponent', () => {
  let component: HelpBenefitsOfTheStudentPassComponent;
  let fixture: ComponentFixture<HelpBenefitsOfTheStudentPassComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpBenefitsOfTheStudentPassComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpBenefitsOfTheStudentPassComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
