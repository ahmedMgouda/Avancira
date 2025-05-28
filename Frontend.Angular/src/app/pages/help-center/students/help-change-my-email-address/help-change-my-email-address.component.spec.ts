import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpChangeMyEmailAddressComponent } from './help-change-my-email-address.component';

describe('HelpChangeMyEmailAddressComponent', () => {
  let component: HelpChangeMyEmailAddressComponent;
  let fixture: ComponentFixture<HelpChangeMyEmailAddressComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpChangeMyEmailAddressComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpChangeMyEmailAddressComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
