import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';

import { SignupComponent } from './signup.component';
import { AuthService } from '../../services/auth.service';
import { SpinnerService } from '../../services/spinner.service';
import { ToastrService } from 'ngx-toastr';
import { SocialProvider } from '../../models/social-provider';

describe('SignupComponent', () => {
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;
  let spinner: jasmine.SpyObj<SpinnerService>;
  let toastr: jasmine.SpyObj<ToastrService>;

  async function createComponent(returnUrl?: string): Promise<{
    fixture: ComponentFixture<SignupComponent>;
    component: SignupComponent;
  }> {
    await TestBed.configureTestingModule({
      imports: [SignupComponent],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
        { provide: SpinnerService, useValue: spinner },
        { provide: ToastrService, useValue: toastr },
        { provide: ActivatedRoute, useValue: { queryParams: of(returnUrl ? { returnUrl } : {}) } },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(SignupComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    return { fixture, component };
  }

  beforeEach(() => {
    authService = jasmine.createSpyObj('AuthService', ['register', 'startLogin']);
    router = jasmine.createSpyObj('Router', ['navigateByUrl']);
    spinner = jasmine.createSpyObj('SpinnerService', ['show', 'hide']);
    toastr = jasmine.createSpyObj('ToastrService', ['error']);
  });

  it('should create', async () => {
    const { component } = await createComponent();
    expect(component).toBeTruthy();
  });

  it('should initiate Google signup flow', async () => {
    const { component } = await createComponent();
    authService.startLogin.and.stub();
    component.returnUrl = '/home';

    component.authenticate(SocialProvider.Google);

    expect(authService.startLogin).toHaveBeenCalledWith('/home');
  });

  it('should initiate Facebook signup flow', async () => {
    const { component } = await createComponent();
    authService.startLogin.and.stub();
    component.returnUrl = '/home';

    component.authenticate(SocialProvider.Facebook);

    expect(authService.startLogin).toHaveBeenCalledWith('/home');
  });

  it('should sanitize external returnUrl', async () => {
    const { component } = await createComponent('https://evil.com');
    expect(component.returnUrl).toBe('/');
  });

  it('should sanitize malformed returnUrl', async () => {
    const { component } = await createComponent('//evil.com');
    expect(component.returnUrl).toBe('/');
  });
});
