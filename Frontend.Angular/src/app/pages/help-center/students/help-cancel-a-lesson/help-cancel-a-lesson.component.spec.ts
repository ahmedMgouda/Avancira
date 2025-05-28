import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpCancelALessonComponent } from './help-cancel-a-lesson.component';

describe('HelpCancelALessonComponent', () => {
  let component: HelpCancelALessonComponent;
  let fixture: ComponentFixture<HelpCancelALessonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpCancelALessonComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpCancelALessonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
