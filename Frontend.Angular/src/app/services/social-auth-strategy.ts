import { SocialProvider } from '../models/social-provider';

export interface SocialAuthStrategy {
  provider: SocialProvider;
  /**
   * Indicates whether the underlying provider has been initialized.
   * Implementations should set this to `true` once initialization succeeds.
   */
  initialized: boolean;
  init(): Promise<void>;
  login(): Promise<string>;
}
