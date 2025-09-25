export interface UserSession {
  id: string;
  userId: string;
  authorizationId: string;
  deviceId: string;
  deviceName?: string;
  userAgent?: string;
  operatingSystem?: string;
  ipAddress: string;
  country?: string;
  city?: string;
  createdAtUtc: string;
  absoluteExpiryUtc: string;
  lastActivityUtc: string;
  revokedAtUtc?: string;
}
