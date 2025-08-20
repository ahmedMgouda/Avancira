import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { ForgotPasswordComponent } from './forgot-password.component';
import { UserService } from '../../services/user.service';

describe('ForgotPasswordComponent', () => {
  let component: ForgotPasswordComponent;
  let fixture: ComponentFixture<ForgotPasswordComponent>;
  let userServiceSpy: jasmine.SpyObj<UserService>;

  beforeEach(async () => {
    userServiceSpy = jasmine.createSpyObj('UserService', ['requestPasswordReset', 'getRequestPasswordResetCooldown']);
    userServiceSpy.requestPasswordReset.and.returnValue(of(void 0));
    userServiceSpy.getRequestPasswordResetCooldown.and.returnValue(0);

    await TestBed.configureTestingModule({
      imports: [ForgotPasswordComponent],
      providers: [{ provide: UserService, useValue: userServiceSpy }]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ForgotPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should set success message on successful submit', async () => {
    component.forgotPasswordForm.setValue({ email: 'test@example.com' });

    await component.onSubmit();

    expect(userServiceSpy.requestPasswordReset).toHaveBeenCalledWith('test@example.com');
    expect(component.successMessage).toBe('A password reset link has been sent to your email.');
    expect(component.errorMessage).toBe('');
    expect(component.isSubmitting).toBeFalse();
  });

  it('should set error message on failed submit', async () => {
    userServiceSpy.requestPasswordReset.and.returnValue(
      throwError(() => ({ error: { message: 'fail' } }))
    );
    component.forgotPasswordForm.setValue({ email: 'test@example.com' });

    await component.onSubmit();

    expect(component.errorMessage).toBe('fail');
    expect(component.successMessage).toBe('');
    expect(component.isSubmitting).toBeFalse();
  });

  it('should disable form if cooldown active on init', fakeAsync(() => {
    userServiceSpy.getRequestPasswordResetCooldown.and.returnValue(2);

    fixture = TestBed.createComponent(ForgotPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.disabled).toBeTrue();

    tick(2000);
    fixture.detectChanges();
    expect(button.disabled).toBeFalse();
  }));
});
