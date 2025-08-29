import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';

import { AuthCallbackComponent } from './auth-callback.component';
import { AuthService } from '../../services/auth.service';

describe('AuthCallbackComponent', () => {
  let fixture: ComponentFixture<AuthCallbackComponent>;
  let auth: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    auth = jasmine.createSpyObj('AuthService', ['init']);
    auth.init.and.returnValue(Promise.resolve());
    router = jasmine.createSpyObj('Router', ['navigateByUrl']);

    await TestBed.configureTestingModule({
      imports: [AuthCallbackComponent],
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({ state: '/home' }) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AuthCallbackComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('initializes auth and navigates to sanitized return url', () => {
    expect(auth.init).toHaveBeenCalled();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/home');
  });
});
