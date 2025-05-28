import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpEditALessonComponent } from './help-edit-a-lesson.component';

describe('HelpEditALessonComponent', () => {
  let component: HelpEditALessonComponent;
  let fixture: ComponentFixture<HelpEditALessonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpEditALessonComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpEditALessonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
