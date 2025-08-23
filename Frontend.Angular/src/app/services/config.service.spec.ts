import { TestBed } from '@angular/core/testing';

import { Config, ConfigService } from './config.service';
import { ConfigKey } from '../models/config-key';

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
        [ConfigKey.StripePublishableKey]: 'spk',
        [ConfigKey.PayPalClientId]: 'ppid',
        [ConfigKey.GoogleMapsApiKey]: 'gmak',
        [ConfigKey.GoogleClientId]: 'gid',
        [ConfigKey.FacebookAppId]: 'fid'
      };

      expect((service as any).isConfigValid(validConfig)).toBeTrue();
    });

    it('should return false when any key is empty', () => {
      const invalidConfig: Config = {
        [ConfigKey.StripePublishableKey]: '',
        [ConfigKey.PayPalClientId]: 'ppid',
        [ConfigKey.GoogleMapsApiKey]: 'gmak',
        [ConfigKey.GoogleClientId]: 'gid',
        [ConfigKey.FacebookAppId]: 'fid'
      };

      expect((service as any).isConfigValid(invalidConfig)).toBeFalse();
    });
  });
});
