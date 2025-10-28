import { PortalNavItem } from '../portal.types';

export const TUTOR_NAV_ITEMS: readonly PortalNavItem[] = [
  { label: 'Dashboard', icon: 'fas fa-home', path: ['dashboard'], exact: true },
  { label: 'Messages', icon: 'fas fa-comments', path: ['messages'], exact: true },
  { label: 'Listings', icon: 'fas fa-clipboard-list', path: ['listings'], exact: true },
  { label: 'Lessons', icon: 'fas fa-chalkboard-teacher', path: ['lessons'], exact: true },
  { label: 'Reviews', icon: 'fas fa-star', path: ['evaluations'], exact: true },
  { label: 'Payments', icon: 'fas fa-credit-card', path: ['payments'], exact: true },
  { label: 'Invoices', icon: 'fas fa-file-invoice', path: ['invoices'], exact: true },
  { label: 'Sessions', icon: 'fas fa-sign-out-alt', path: ['sessions'], exact: true },
  { label: 'Profile', icon: 'fas fa-user-cog', path: ['profile'], exact: true },
  { label: 'Table Test', icon: 'fas fa-table', path: ['tabletest'], exact: true },
];
