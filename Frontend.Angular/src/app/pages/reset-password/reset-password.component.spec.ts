import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ResetPasswordComponent } from './reset-password.component';

describe('ResetPasswordComponent', () => {
  let component: ResetPasswordComponent;
  let fixture: ComponentFixture<ResetPasswordComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResetPasswordComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ResetPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should invalidate password without uppercase letter', () => {
    const control = component.resetPasswordForm.controls['newPassword'];
    control.setValue('alllowercase1!');
    expect(control.valid).toBeFalse();
  });

  it('should invalidate password without lowercase letter', () => {
    const control = component.resetPasswordForm.controls['newPassword'];
    control.setValue('ALLUPPERCASE1!');
    expect(control.valid).toBeFalse();
  });

  it('should invalidate password without digits', () => {
    const control = component.resetPasswordForm.controls['newPassword'];
    control.setValue('NoDigits!');
    expect(control.valid).toBeFalse();
  });

  it('should invalidate password without symbols', () => {
    const control = component.resetPasswordForm.controls['newPassword'];
    control.setValue('NoSymbols1');
    expect(control.valid).toBeFalse();
  });

  it('should validate a strong password', () => {
    const control = component.resetPasswordForm.controls['newPassword'];
    control.setValue('Str0ng!Pass');
    expect(control.valid).toBeTrue();
  });

  it('should invalidate the form when passwords do not match', () => {
    const form = component.resetPasswordForm;
    form.controls['newPassword'].setValue('Str0ng!Pass');
    form.controls['confirmPassword'].setValue('Different1!');
    expect(form.valid).toBeFalse();
    form.controls['confirmPassword'].setValue('Str0ng!Pass');
    expect(form.valid).toBeTrue();
  });
});
