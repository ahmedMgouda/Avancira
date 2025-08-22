import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { SigninComponent } from './signin.component';
import { AuthService } from '../../services/auth.service';
import { SpinnerService } from '../../services/spinner.service';
import { ToastrService } from 'ngx-toastr';
import { AlertService } from '../../services/alert.service';
import { UserService } from '../../services/user.service';
import { SocialAuthService } from '../../services/social-auth.service';

describe('LoginComponent', () => {
  let component: SigninComponent;
  let fixture: ComponentFixture<SigninComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let socialAuth: jasmine.SpyObj<SocialAuthService>;
  let router: jasmine.SpyObj<Router>;
  let spinner: jasmine.SpyObj<SpinnerService>;
  let toastr: jasmine.SpyObj<ToastrService>;

  beforeEach(async () => {
    authService = jasmine.createSpyObj('AuthService', ['login']);
    socialAuth = jasmine.createSpyObj('SocialAuthService', ['authenticate']);
    router = jasmine.createSpyObj('Router', ['navigateByUrl']);
    spinner = jasmine.createSpyObj('SpinnerService', ['show', 'hide']);
    toastr = jasmine.createSpyObj('ToastrService', ['error']);

    await TestBed.configureTestingModule({
      imports: [SigninComponent],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: SocialAuthService, useValue: socialAuth },
        { provide: Router, useValue: router },
        { provide: SpinnerService, useValue: spinner },
        { provide: ToastrService, useValue: toastr },
        { provide: ActivatedRoute, useValue: { queryParams: of({}) } },
        { provide: AlertService, useValue: {} },
        { provide: UserService, useValue: {} },
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
    socialAuth.authenticate.and.returnValue(of({} as any));
    component.returnUrl = '/home';

    component.authenticate('google');

    expect(socialAuth.authenticate).toHaveBeenCalledWith('google');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/home');
    expect(spinner.hide).toHaveBeenCalled();
  });

  it('should show error toast and hide spinner on social login error', () => {
    socialAuth.authenticate.and.returnValue(throwError(() => new Error('fail')));

    component.authenticate('google');

    expect(toastr.error).toHaveBeenCalled();
    expect(spinner.hide).toHaveBeenCalled();
  });
});
