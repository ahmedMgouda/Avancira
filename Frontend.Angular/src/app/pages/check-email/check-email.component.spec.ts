import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CheckEmailComponent } from './check-email.component';
import { AuthService } from '../../services/auth.service';

describe('CheckEmailComponent', () => {
  let component: CheckEmailComponent;
  let fixture: ComponentFixture<CheckEmailComponent>;
  const authServiceMock = {
    startLogin: jasmine.createSpy('startLogin')
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CheckEmailComponent],
      providers: [{ provide: AuthService, useValue: authServiceMock }]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CheckEmailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
