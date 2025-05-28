import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HelpChangeOrResetMyPasswordComponent } from './help-change-or-reset-my-password.component';

describe('HelpChangeOrResetMyPasswordComponent', () => {
  let component: HelpChangeOrResetMyPasswordComponent;
  let fixture: ComponentFixture<HelpChangeOrResetMyPasswordComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HelpChangeOrResetMyPasswordComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HelpChangeOrResetMyPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
