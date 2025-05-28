import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpReportALessonComponent } from './help-tutors-report-a-lesson.component';

describe('HelpReportALessonComponent', () => {
  let component: HelpReportALessonComponent;
  let fixture: ComponentFixture<HelpReportALessonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpReportALessonComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpReportALessonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
