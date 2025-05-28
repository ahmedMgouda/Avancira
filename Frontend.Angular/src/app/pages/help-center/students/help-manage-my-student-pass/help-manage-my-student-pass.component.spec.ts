import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpManageMyStudentPassComponent } from './help-manage-my-student-pass.component';

describe('HelpManageMyStudentPassComponent', () => {
  let component: HelpManageMyStudentPassComponent;
  let fixture: ComponentFixture<HelpManageMyStudentPassComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpManageMyStudentPassComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpManageMyStudentPassComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
