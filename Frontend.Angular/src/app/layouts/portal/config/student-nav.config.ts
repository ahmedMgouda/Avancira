import { PortalNavItem } from '../portal.types';

export const STUDENT_NAV_ITEMS: readonly PortalNavItem[] = [
  { label: 'Dashboard', icon: 'fas fa-home', path: ['dashboard'], exact: true },
  { label: 'Messages', icon: 'fas fa-comments', path: ['messages'], exact: true },
  { label: 'Lessons', icon: 'fas fa-chalkboard-teacher', path: ['lessons'], exact: true },
  { label: 'Reviews', icon: 'fas fa-star', path: ['evaluations'], exact: true },
  { label: 'Payments', icon: 'fas fa-credit-card', path: ['payments'], exact: true },
  { label: 'Invoices', icon: 'fas fa-file-invoice', path: ['invoices'], exact: true },
  { label: 'Profile', icon: 'fas fa-user-cog', path: ['profile'], exact: true },
];