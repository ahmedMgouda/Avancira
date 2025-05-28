import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpTutorsFirstLessonFreeComponent } from './help-tutors-first-lesson-free.component';

describe('HelpFirstLessonFreeComponent', () => {
  let component: HelpTutorsFirstLessonFreeComponent;
  let fixture: ComponentFixture<HelpTutorsFirstLessonFreeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpTutorsFirstLessonFreeComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpTutorsFirstLessonFreeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
