import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpAddOrModifyMyPhoneNumberComponent } from './help-add-or-modify-my-phone-number.component';

describe('HelpAddOrModifyMyPhoneNumberComponent', () => {
  let component: HelpAddOrModifyMyPhoneNumberComponent;
  let fixture: ComponentFixture<HelpAddOrModifyMyPhoneNumberComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpAddOrModifyMyPhoneNumberComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpAddOrModifyMyPhoneNumberComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
