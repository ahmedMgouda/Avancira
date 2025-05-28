import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpManageMyNotificationsComponent } from './help-manage-my-notifications.component';

describe('HelpManageMyNotificationsComponent', () => {
  let component: HelpManageMyNotificationsComponent;
  let fixture: ComponentFixture<HelpManageMyNotificationsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpManageMyNotificationsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpManageMyNotificationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
