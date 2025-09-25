import { UserSession } from './session';

export interface DeviceSessions {
  deviceId: string;
  deviceName?: string;
  operatingSystem?: string;
  userAgent?: string;
  country?: string;
  city?: string;
  lastActivityUtc: string;
  sessions: UserSession[];
}
