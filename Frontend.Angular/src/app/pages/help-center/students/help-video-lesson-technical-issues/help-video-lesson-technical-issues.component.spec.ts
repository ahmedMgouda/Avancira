import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpVideoLessonTechnicalIssuesComponent } from './help-video-lesson-technical-issues.component';

describe('HelpVideoLessonTechnicalIssuesComponent', () => {
  let component: HelpVideoLessonTechnicalIssuesComponent;
  let fixture: ComponentFixture<HelpVideoLessonTechnicalIssuesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpVideoLessonTechnicalIssuesComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpVideoLessonTechnicalIssuesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
