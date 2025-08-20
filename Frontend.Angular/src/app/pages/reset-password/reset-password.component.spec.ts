import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';

import { ResetPasswordComponent } from './reset-password.component';

describe('ResetPasswordComponent', () => {
  let component: ResetPasswordComponent;
  let fixture: ComponentFixture<ResetPasswordComponent>;
  let queryParamsSubject: Subject<any>;

  beforeEach(async () => {
    queryParamsSubject = new Subject();

    await TestBed.configureTestingModule({
      imports: [ResetPasswordComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { queryParams: queryParamsSubject.asObservable() } },
      ],
    }).compileComponents();

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

  it('should unsubscribe from query params on destroy', () => {
    queryParamsSubject.next({ token: 'first', userId: 'one' });
    expect(component.token).toBe('first');
    expect(component.userId).toBe('one');

    component.ngOnDestroy();
    queryParamsSubject.next({ token: 'second', userId: 'two' });

    expect(component.token).toBe('first');
    expect(component.userId).toBe('one');
  });
});
