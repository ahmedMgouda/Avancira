import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpChangeMyPayoutPreferenceComponent } from './help-change-my-payout-preference.component';

describe('HelpChangeMyPayoutPreferenceComponent', () => {
  let component: HelpChangeMyPayoutPreferenceComponent;
  let fixture: ComponentFixture<HelpChangeMyPayoutPreferenceComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpChangeMyPayoutPreferenceComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpChangeMyPayoutPreferenceComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
