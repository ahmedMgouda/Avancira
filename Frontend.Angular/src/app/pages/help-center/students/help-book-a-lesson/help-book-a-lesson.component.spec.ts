import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpBookALessonComponent } from './help-book-a-lesson.component';

describe('HelpBookALessonComponent', () => {
  let component: HelpBookALessonComponent;
  let fixture: ComponentFixture<HelpBookALessonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpBookALessonComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpBookALessonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
