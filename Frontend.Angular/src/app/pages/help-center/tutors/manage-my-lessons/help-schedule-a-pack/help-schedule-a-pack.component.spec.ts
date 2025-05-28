import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpScheduleAPackComponent } from './help-schedule-a-pack.component';

describe('HelpScheduleAPackComponent', () => {
  let component: HelpScheduleAPackComponent;
  let fixture: ComponentFixture<HelpScheduleAPackComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpScheduleAPackComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpScheduleAPackComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
