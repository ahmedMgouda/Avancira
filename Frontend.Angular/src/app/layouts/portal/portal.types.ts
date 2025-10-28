export type PortalRole = 'student' | 'tutor' | 'admin';
export type PortalNavPath = readonly string[];

export interface PortalNavItem {
  label: string;
  icon: string;
  path: PortalNavPath;
  exact?: boolean;
}

export interface NavigationConfig {
  student: readonly PortalNavItem[];
  tutor: readonly PortalNavItem[];
  admin: readonly PortalNavItem[];
}
