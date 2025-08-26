import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';

import { SignupComponent } from './signup.component';
import { AuthService } from '../../services/auth.service';
import { SpinnerService } from '../../services/spinner.service';
import { ToastrService } from 'ngx-toastr';
import { SocialProvider } from '../../models/social-provider';

describe('SignupComponent', () => {
  let component: SignupComponent;
  let fixture: ComponentFixture<SignupComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;
  let spinner: jasmine.SpyObj<SpinnerService>;
  let toastr: jasmine.SpyObj<ToastrService>;

  beforeEach(async () => {
    authService = jasmine.createSpyObj('AuthService', ['register', 'startLogin']);
    router = jasmine.createSpyObj('Router', ['navigateByUrl']);
    spinner = jasmine.createSpyObj('SpinnerService', ['show', 'hide']);
    toastr = jasmine.createSpyObj('ToastrService', ['error']);

    await TestBed.configureTestingModule({
      imports: [SignupComponent],
      providers: [
        { provide: AuthService, useValue: authService },
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

  it('should initiate Google signup flow', () => {
    authService.startLogin.and.stub();
    component.returnUrl = '/home';

    component.authenticate(SocialProvider.Google);

    expect(authService.startLogin).toHaveBeenCalledWith('/home', SocialProvider.Google);
  });

  it('should initiate Facebook signup flow', () => {
    authService.startLogin.and.stub();
    component.returnUrl = '/home';

    component.authenticate(SocialProvider.Facebook);

    expect(authService.startLogin).toHaveBeenCalledWith('/home', SocialProvider.Facebook);
  });
});
