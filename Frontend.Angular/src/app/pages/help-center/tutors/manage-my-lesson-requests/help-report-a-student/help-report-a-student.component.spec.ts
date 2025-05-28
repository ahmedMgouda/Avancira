import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpReportAStudentComponent } from './help-report-a-student.component';

describe('HelpReportAStudentComponent', () => {
  let component: HelpReportAStudentComponent;
  let fixture: ComponentFixture<HelpReportAStudentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpReportAStudentComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpReportAStudentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
