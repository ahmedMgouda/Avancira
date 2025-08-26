import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';

import { SigninComponent } from './signin.component';
import { AuthService } from '../../services/auth.service';
import { ToastrService } from 'ngx-toastr';
import { AlertService } from '../../services/alert.service';
import { UserService } from '../../services/user.service';
import { SocialProvider } from '../../models/social-provider';

describe('LoginComponent', () => {
  let component: SigninComponent;
  let fixture: ComponentFixture<SigninComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;
  let toastr: jasmine.SpyObj<ToastrService>;

  beforeEach(async () => {
    authService = jasmine.createSpyObj('AuthService', ['startLogin']);
    router = jasmine.createSpyObj('Router', ['navigateByUrl']);
    toastr = jasmine.createSpyObj('ToastrService', ['error']);

    await TestBed.configureTestingModule({
      imports: [SigninComponent],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
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

  it('should initiate Google login flow', () => {
    authService.startLogin.and.stub();
    component.returnUrl = '/home';

    component.authenticate(SocialProvider.Google);

    expect(authService.startLogin).toHaveBeenCalledWith('/home', SocialProvider.Google);
  });

  it('should initiate Facebook login flow', () => {
    authService.startLogin.and.stub();
    component.returnUrl = '/home';

    component.authenticate(SocialProvider.Facebook);

    expect(authService.startLogin).toHaveBeenCalledWith('/home', SocialProvider.Facebook);
  });
});
