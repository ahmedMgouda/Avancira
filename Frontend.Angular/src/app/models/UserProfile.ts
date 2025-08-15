export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName?: string;
  timeZoneId?: string;
  ipAddress?: string;
  imageUrl?: string;
  deviceId?: string;
  roles: string[];        // multiple roles
  permissions: string[];  // permissions as strings
  exp?: number;           // token expiration (epoch seconds)
}
