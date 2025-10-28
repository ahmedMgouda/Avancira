import { PortalNavPath,PortalRole } from '../../portal/portal.types';

export function buildRoleLink(role: PortalRole | null, path: PortalNavPath): any[] {
  if (!role) {
    return ['/'];
  }
  return ['/', role, ...path];
}

export function getRoleDashboardLink(role: PortalRole | null): string[] {
  if (!role) {
    return ['/'];
  }
  return ['/', role, 'dashboard'];
}

export function getRoleProfileLink(role: PortalRole | null): string[] {
  if (!role) {
    return ['/'];
  }
  return ['/', role, 'profile'];
}

export const LINK_ACTIVE_OPTIONS = {
  exact: { exact: true } as const,
  partial: { exact: false } as const
};