import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpManageMySubscriptionComponent } from './help-manage-my-subscription.component';

describe('HelpManageMySubscriptionComponent', () => {
  let component: HelpManageMySubscriptionComponent;
  let fixture: ComponentFixture<HelpManageMySubscriptionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpManageMySubscriptionComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpManageMySubscriptionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
