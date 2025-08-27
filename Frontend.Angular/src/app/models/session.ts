export interface Session {
  id: string;
  device: string;
  userAgent?: string;
  operatingSystem?: string;
  ipAddress: string;
  country?: string;
  city?: string;
  createdUtc: string;
  lastActivityUtc: string;
  lastRefreshUtc: string;
  absoluteExpiryUtc: string;
  revokedUtc?: string;
}
