import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpUpdateMyAdComponent } from './help-update-my-ad.component';

describe('HelpUpdateMyAdComponent', () => {
  let component: HelpUpdateMyAdComponent;
  let fixture: ComponentFixture<HelpUpdateMyAdComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpUpdateMyAdComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpUpdateMyAdComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
