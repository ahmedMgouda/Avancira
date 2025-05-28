import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpMyAdsVisibilityComponent } from './help-my-ads-visibility.component';

describe('HelpMyAdsVisibilityComponent', () => {
  let component: HelpMyAdsVisibilityComponent;
  let fixture: ComponentFixture<HelpMyAdsVisibilityComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpMyAdsVisibilityComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpMyAdsVisibilityComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
