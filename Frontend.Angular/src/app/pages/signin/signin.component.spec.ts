import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { SigninComponent } from './signin.component';
import { AuthService } from '../../services/auth.service';
import { SpinnerService } from '../../services/spinner.service';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from '../../services/config.service';
import { AlertService } from '../../services/alert.service';
import { UserService } from '../../services/user.service';
import { GoogleAuthService } from '../../services/google-auth.service';
import { FacebookService } from 'ngx-facebook';

describe('LoginComponent', () => {
  let component: SigninComponent;
  let fixture: ComponentFixture<SigninComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;
  let spinner: jasmine.SpyObj<SpinnerService>;
  let toastr: jasmine.SpyObj<ToastrService>;

  beforeEach(async () => {
    authService = jasmine.createSpyObj('AuthService', ['externalLogin']);
    router = jasmine.createSpyObj('Router', ['navigateByUrl']);
    spinner = jasmine.createSpyObj('SpinnerService', ['show', 'hide']);
    toastr = jasmine.createSpyObj('ToastrService', ['error']);

    await TestBed.configureTestingModule({
      imports: [SigninComponent],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
        { provide: SpinnerService, useValue: spinner },
        { provide: ToastrService, useValue: toastr },
        { provide: ActivatedRoute, useValue: { queryParams: of({}) } },
        { provide: ConfigService, useValue: { loadConfig: () => of({}), get: () => '' } },
        { provide: AlertService, useValue: {} },
        { provide: UserService, useValue: {} },
        { provide: GoogleAuthService, useValue: {} },
        { provide: FacebookService, useValue: { init: () => {} } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SigninComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should navigate to returnUrl on successful social login', () => {
    authService.externalLogin.and.returnValue(of({} as any));
    component.returnUrl = '/home';

    component.handleSocialLogin('google', 'token123');

    expect(authService.externalLogin).toHaveBeenCalledWith('google', 'token123');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/home');
    expect(spinner.hide).toHaveBeenCalled();
  });

  it('should show error toast and hide spinner on social login error', () => {
    authService.externalLogin.and.returnValue(throwError(() => new Error('fail')));

    component.handleSocialLogin('google', 'token123');

    expect(toastr.error).toHaveBeenCalled();
    expect(spinner.hide).toHaveBeenCalled();
  });
});
