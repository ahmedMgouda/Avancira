import { Routes } from '@angular/router';

import { adminRoutes } from './app.routes.admin';
import { publicRoutes } from './app.routes.public';
import { studentRoutes } from './app.routes.student';
import { tutorRoutes } from './app.routes.tutor';

import { environment } from '@/environments/environment';

export const routes: Routes = [
  ...publicRoutes,
  ...studentRoutes,
  ...tutorRoutes,
  ...adminRoutes,
  // ────────────────────────────────────────────────────────────────
  // Developer Monitor (only available in non-production)
  // ────────────────────────────────────────────────────────────────
  {
    path: 'dev-monitor',
    loadChildren: () =>
      !environment.production
        ? import('../pages/dev-monitor/dev-monitor.routes').then(m => m.DEV_MONITOR_ROUTES)
        : Promise.resolve([]),
  },
  {
    path: 'error',
    loadComponent: () =>
      import('../pages/error/error.component').then(m => m.ErrorComponent),
    data: { title: 'Error' }
  },

  // ────────────────────────────────────────────────────────────────
  // Fallback
  // ────────────────────────────────────────────────────────────────
  {
    path: '**',
    redirectTo: '',
  },
];