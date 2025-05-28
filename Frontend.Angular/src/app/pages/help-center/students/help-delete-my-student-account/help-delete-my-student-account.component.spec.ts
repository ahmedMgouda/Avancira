import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpDeleteMyStudentAccountComponent } from './help-delete-my-student-account.component';

describe('HelpDeleteMyStudentAccountComponent', () => {
  let component: HelpDeleteMyStudentAccountComponent;
  let fixture: ComponentFixture<HelpDeleteMyStudentAccountComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpDeleteMyStudentAccountComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpDeleteMyStudentAccountComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
