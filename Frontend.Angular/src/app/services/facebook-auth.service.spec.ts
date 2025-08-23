import { TestBed } from '@angular/core/testing';
import { FacebookAuthService } from './facebook-auth.service';
import { ConfigService } from './config.service';
import { FacebookService } from 'ngx-facebook';

describe('FacebookAuthService', () => {
  let service: FacebookAuthService;
  let config: jasmine.SpyObj<ConfigService>;
  let fb: jasmine.SpyObj<FacebookService>;

  beforeEach(() => {
    config = jasmine.createSpyObj('ConfigService', ['get']);
    fb = jasmine.createSpyObj('FacebookService', ['init']);

    TestBed.configureTestingModule({
      providers: [
        FacebookAuthService,
        { provide: ConfigService, useValue: config },
        { provide: FacebookService, useValue: fb },
      ],
    });

    service = TestBed.inject(FacebookAuthService);
  });

  it('should reject when facebookAppId is undefined', async () => {
    config.get.and.returnValue(undefined as any);
    await expectAsync(service.ensureInitialized()).toBeRejectedWithError('Facebook App ID is required.');
    expect(fb.init).not.toHaveBeenCalled();
  });

  it('should reject when facebookAppId is empty', async () => {
    config.get.and.returnValue('');
    await expectAsync(service.ensureInitialized()).toBeRejectedWithError('Facebook App ID is required.');
    expect(fb.init).not.toHaveBeenCalled();
  });
});
