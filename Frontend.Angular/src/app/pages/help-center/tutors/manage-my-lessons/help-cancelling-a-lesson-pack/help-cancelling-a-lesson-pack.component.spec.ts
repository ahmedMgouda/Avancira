import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpCancellingALessonPackComponent } from './help-cancelling-a-lesson-pack.component';

describe('HelpCancellingALessonPackComponent', () => {
  let component: HelpCancellingALessonPackComponent;
  let fixture: ComponentFixture<HelpCancellingALessonPackComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpCancellingALessonPackComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpCancellingALessonPackComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
