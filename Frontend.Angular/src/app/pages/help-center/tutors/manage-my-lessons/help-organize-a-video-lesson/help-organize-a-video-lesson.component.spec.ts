import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpOrganizeAVideoLessonComponent } from './help-organize-a-video-lesson.component';

describe('HelpOrganizeAVideoLessonComponent', () => {
  let component: HelpOrganizeAVideoLessonComponent;
  let fixture: ComponentFixture<HelpOrganizeAVideoLessonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpOrganizeAVideoLessonComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpOrganizeAVideoLessonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
