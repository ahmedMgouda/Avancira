import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpFirstLessonFreeComponent } from './help-first-lesson-free.component';

describe('HelpFirstLessonFreeComponent', () => {
  let component: HelpFirstLessonFreeComponent;
  let fixture: ComponentFixture<HelpFirstLessonFreeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpFirstLessonFreeComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpFirstLessonFreeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
