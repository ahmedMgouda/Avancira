import { TestBed } from '@angular/core/testing';

import { Config, ConfigService } from './config.service';

describe('ConfigService', () => {
  let service: ConfigService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ConfigService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('isConfigValid', () => {
    it('should return true when all keys are populated', () => {
      const validConfig: Config = {
        stripePublishableKey: 'spk',
        payPalClientId: 'ppid',
        googleMapsApiKey: 'gmak',
        googleClientId: 'gid',
        facebookAppId: 'fid',
        enabledSocialProviders: []
      };

      expect((service as any).isConfigValid(validConfig)).toBeTrue();
    });

    it('should return false when any key is empty', () => {
      const invalidConfig: Config = {
        stripePublishableKey: '',
        payPalClientId: 'ppid',
        googleMapsApiKey: 'gmak',
        googleClientId: 'gid',
        facebookAppId: 'fid',
        enabledSocialProviders: []
      };

      expect((service as any).isConfigValid(invalidConfig)).toBeFalse();
    });
  });
});
