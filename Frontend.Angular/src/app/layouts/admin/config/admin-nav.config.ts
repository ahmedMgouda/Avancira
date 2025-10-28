import { PortalNavItem } from '../../portal/portal.types';

export const ADMIN_NAV_ITEMS: readonly PortalNavItem[] = [
  { label: 'Dashboard', icon: 'fas fa-home', path: ['dashboard'], exact: true },
  {
    label: 'Categories',
    icon: 'fas fa-tags',
    path: ['categories'],
    exact: false,
  },
  { label: 'Users', icon: 'fas fa-users', path: ['users'], exact: true },
  { label: 'Tutors', icon: 'fas fa-chalkboard-teacher', path: ['tutors'], exact: true },
  { label: 'Lessons', icon: 'fas fa-book', path: ['lessons'], exact: true },
  { label: 'Reports', icon: 'fas fa-chart-line', path: ['reports'], exact: true },
  { label: 'Settings', icon: 'fas fa-cog', path: ['settings'], exact: true },
];
