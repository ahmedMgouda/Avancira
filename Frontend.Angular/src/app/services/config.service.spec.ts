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
    it('should treat config with only social keys as valid', () => {
      const socialConfig: Config = {
        stripePublishableKey: '',
        payPalClientId: '',
        googleMapsApiKey: '',
        googleClientId: 'gid',
        facebookAppId: 'fid',
        enabledSocialProviders: []
      };

      expect((service as any).isConfigValid(socialConfig)).toBeTrue();
    });

    it('should treat config with empty social keys as invalid', () => {
      const emptySocialConfig: Config = {
        stripePublishableKey: '',
        payPalClientId: '',
        googleMapsApiKey: '',
        googleClientId: '',
        facebookAppId: '',
        enabledSocialProviders: []
      };

      expect((service as any).isConfigValid(emptySocialConfig)).toBeFalse();
    });
  });
});
