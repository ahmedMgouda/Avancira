import { SocialProvider } from '../models/social-provider';

export interface SocialAuthStrategy {
  provider: SocialProvider;
  init(): Promise<void>;
  login(): Promise<string>;
}
