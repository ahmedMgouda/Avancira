import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpStartAVideoClassComponent } from './help-start-a-video-class.component';

describe('HelpStartAVideoClassComponent', () => {
  let component: HelpStartAVideoClassComponent;
  let fixture: ComponentFixture<HelpStartAVideoClassComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpStartAVideoClassComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpStartAVideoClassComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
