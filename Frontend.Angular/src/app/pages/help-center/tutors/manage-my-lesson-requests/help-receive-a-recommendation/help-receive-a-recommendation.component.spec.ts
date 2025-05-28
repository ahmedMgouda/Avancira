import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpReceiveARecommendationComponent } from './help-receive-a-recommendation.component';

describe('HelpReceiveARecommendationComponent', () => {
  let component: HelpReceiveARecommendationComponent;
  let fixture: ComponentFixture<HelpReceiveARecommendationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpReceiveARecommendationComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpReceiveARecommendationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
