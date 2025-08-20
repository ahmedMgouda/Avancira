import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';

import { ResetPasswordComponent } from './reset-password.component';
import { UserService } from '../../services/user.service';

describe('ResetPasswordComponent', () => {
  let component: ResetPasswordComponent;
  let fixture: ComponentFixture<ResetPasswordComponent>;
  let queryParamsSubject: Subject<any>;
  let userServiceSpy: jasmine.SpyObj<UserService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    queryParamsSubject = new Subject();
    userServiceSpy = jasmine.createSpyObj('UserService', ['resetPassword']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    userServiceSpy.resetPassword.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [ResetPasswordComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { queryParams: queryParamsSubject.asObservable() } },
        { provide: UserService, useValue: userServiceSpy },
        { provide: Router, useValue: routerSpy },
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

  it('should set success message on successful reset', async () => {
    component.token = 'token';
    component.userId = '1';
    component.resetPasswordForm.setValue({
      newPassword: 'Str0ng!Pass',
      confirmPassword: 'Str0ng!Pass',
    });

    spyOn(window, 'setTimeout').and.callFake((fn: Function) => fn());

    await component.resetPassword();

    expect(userServiceSpy.resetPassword).toHaveBeenCalled();
    expect(component.successMessage).toBe('Your password has been reset successfully!');
    expect(component.errorMessage).toBe('');
    expect(component.isSubmitting).toBeFalse();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/signin']);
  });

  it('should set error message on failed reset', async () => {
    userServiceSpy.resetPassword.and.returnValue(
      throwError(() => ({ error: { message: 'oops' } }))
    );
    component.token = 'token';
    component.userId = '1';
    component.resetPasswordForm.setValue({
      newPassword: 'Str0ng!Pass',
      confirmPassword: 'Str0ng!Pass',
    });

    await component.resetPassword();

    expect(component.errorMessage).toBe('oops');
    expect(component.successMessage).toBe('');
    expect(component.isSubmitting).toBeFalse();
  });
});
