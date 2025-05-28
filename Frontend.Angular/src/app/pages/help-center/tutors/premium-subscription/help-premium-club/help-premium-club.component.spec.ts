import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpPremiumClubComponent } from './help-premium-club.component';

describe('HelpPremiumClubComponent', () => {
  let component: HelpPremiumClubComponent;
  let fixture: ComponentFixture<HelpPremiumClubComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpPremiumClubComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpPremiumClubComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
