import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { SignupComponent } from './signup.component';
import { AuthService } from '../../services/auth.service';
import { SpinnerService } from '../../services/spinner.service';
import { ToastrService } from 'ngx-toastr';
import { SocialAuthService } from '../../services/social-auth.service';
import { SocialProvider } from '../../models/social-provider';

describe('SignupComponent', () => {
  let component: SignupComponent;
  let fixture: ComponentFixture<SignupComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let socialAuth: jasmine.SpyObj<SocialAuthService>;
  let router: jasmine.SpyObj<Router>;
  let spinner: jasmine.SpyObj<SpinnerService>;
  let toastr: jasmine.SpyObj<ToastrService>;

  beforeEach(async () => {
    authService = jasmine.createSpyObj('AuthService', ['register']);
    socialAuth = jasmine.createSpyObj('SocialAuthService', ['authenticate']);
    router = jasmine.createSpyObj('Router', ['navigateByUrl']);
    spinner = jasmine.createSpyObj('SpinnerService', ['show', 'hide']);
    toastr = jasmine.createSpyObj('ToastrService', ['error']);

    await TestBed.configureTestingModule({
      imports: [SignupComponent],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: SocialAuthService, useValue: socialAuth },
        { provide: Router, useValue: router },
        { provide: SpinnerService, useValue: spinner },
        { provide: ToastrService, useValue: toastr },
        { provide: ActivatedRoute, useValue: { queryParams: of({}) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SignupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should navigate to returnUrl on successful Google signup', () => {
    socialAuth.authenticate.and.returnValue(of({} as any));
    component.returnUrl = '/home';

    component.authenticate(SocialProvider.Google);

    expect(socialAuth.authenticate).toHaveBeenCalledWith(SocialProvider.Google);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/home');
    expect(spinner.hide).toHaveBeenCalled();
  });

  it('should navigate to returnUrl on successful Facebook signup', () => {
    socialAuth.authenticate.and.returnValue(of({} as any));
    component.returnUrl = '/home';

    component.authenticate(SocialProvider.Facebook);

    expect(socialAuth.authenticate).toHaveBeenCalledWith(SocialProvider.Facebook);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/home');
    expect(spinner.hide).toHaveBeenCalled();
  });

  it('should show error toast and hide spinner on social signup error', () => {
    socialAuth.authenticate.and.returnValue(throwError(() => new Error('fail')));

    component.authenticate(SocialProvider.Google);

    expect(toastr.error).toHaveBeenCalled();
    expect(spinner.hide).toHaveBeenCalled();
  });
});
