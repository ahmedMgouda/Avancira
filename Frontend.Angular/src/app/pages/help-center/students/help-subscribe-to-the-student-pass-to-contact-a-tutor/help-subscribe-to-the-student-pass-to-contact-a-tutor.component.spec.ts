import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpSubscribeToTheStudentPassToContactATutorComponent } from './help-subscribe-to-the-student-pass-to-contact-a-tutor.component';

describe('HelpSubscribeToTheStudentPassToContactATutorComponent', () => {
  let component: HelpSubscribeToTheStudentPassToContactATutorComponent;
  let fixture: ComponentFixture<HelpSubscribeToTheStudentPassToContactATutorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpSubscribeToTheStudentPassToContactATutorComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpSubscribeToTheStudentPassToContactATutorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
