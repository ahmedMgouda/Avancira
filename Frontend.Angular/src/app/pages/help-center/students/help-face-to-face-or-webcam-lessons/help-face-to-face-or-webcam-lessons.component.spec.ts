import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpFaceToFaceOrWebcamLessonsComponent } from './help-face-to-face-or-webcam-lessons.component';

describe('HelpFaceToFaceOrWebcamLessonsComponent', () => {
  let component: HelpFaceToFaceOrWebcamLessonsComponent;
  let fixture: ComponentFixture<HelpFaceToFaceOrWebcamLessonsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpFaceToFaceOrWebcamLessonsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpFaceToFaceOrWebcamLessonsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
