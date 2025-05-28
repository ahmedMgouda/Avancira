import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpHowToGetPaidComponent } from './help-how-to-get-paid.component';

describe('HelpHowToGetPaidComponent', () => {
  let component: HelpHowToGetPaidComponent;
  let fixture: ComponentFixture<HelpHowToGetPaidComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpHowToGetPaidComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpHowToGetPaidComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
