import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpModifyOrCancelALessonComponent } from './help-modify-or-cancel-a-lesson.component';

describe('HelpModifyOrCancelALessonComponent', () => {
  let component: HelpModifyOrCancelALessonComponent;
  let fixture: ComponentFixture<HelpModifyOrCancelALessonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpModifyOrCancelALessonComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpModifyOrCancelALessonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
