import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';

import { SigninComponent } from './signin.component';
import { AuthService } from '../../services/auth.service';

describe('SigninComponent', () => {
  let fixture: ComponentFixture<SigninComponent>;
  let auth: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    auth = jasmine.createSpyObj('AuthService', ['startLogin']);

    await TestBed.configureTestingModule({
      imports: [SigninComponent],
      providers: [
        { provide: AuthService, useValue: auth },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({ returnUrl: '/home' }) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SigninComponent);
    fixture.detectChanges();
  });

  it('starts login on init with sanitized return url', () => {
    expect(auth.startLogin).toHaveBeenCalledWith('/home');
  });
});
