import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { of } from 'rxjs';

import { ForgotPasswordComponent } from './forgot-password.component';
import { UserService } from '../../services/user.service';

describe('ForgotPasswordComponent e2e', () => {
  let component: ForgotPasswordComponent;
  let fixture: ComponentFixture<ForgotPasswordComponent>;
  let userServiceSpy: jasmine.SpyObj<UserService>;

  beforeEach(async () => {
    userServiceSpy = jasmine.createSpyObj('UserService', ['requestPasswordReset', 'getRequestPasswordResetCooldown']);
    userServiceSpy.getRequestPasswordResetCooldown.and.returnValue(0);
    userServiceSpy.requestPasswordReset.and.callFake(() => {
      userServiceSpy.getRequestPasswordResetCooldown.and.returnValue(3);
      return of(void 0);
    });

    await TestBed.configureTestingModule({
      imports: [ForgotPasswordComponent],
      providers: [{ provide: UserService, useValue: userServiceSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(ForgotPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('disables the submit button during cooldown after submission', fakeAsync(() => {
    component.forgotPasswordForm.setValue({ email: 'test@example.com' });
    component.onSubmit();
    tick();
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.disabled).toBeTrue();

    tick(3000);
    fixture.detectChanges();
    expect(button.disabled).toBeFalse();
  }));
});
