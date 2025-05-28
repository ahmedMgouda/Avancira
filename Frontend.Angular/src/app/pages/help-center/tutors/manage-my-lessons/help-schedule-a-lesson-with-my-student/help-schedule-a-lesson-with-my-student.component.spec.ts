import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpScheduleALessonWithMyStudentComponent } from './help-schedule-a-lesson-with-my-student.component';

describe('HelpScheduleALessonWithMyStudentComponent', () => {
  let component: HelpScheduleALessonWithMyStudentComponent;
  let fixture: ComponentFixture<HelpScheduleALessonWithMyStudentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpScheduleALessonWithMyStudentComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpScheduleALessonWithMyStudentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
