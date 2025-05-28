import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProposeLessonComponent } from './propose-lesson.component';

describe('ProposeLessonComponent', () => {
  let component: ProposeLessonComponent;
  let fixture: ComponentFixture<ProposeLessonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProposeLessonComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProposeLessonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
