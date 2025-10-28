import { Routes } from '@angular/router';

import { AdminShellComponent } from '../layouts/admin/admin-shell.component';

import { roleGuard } from '../guards/role.guard';

export const adminRoutes: Routes = [
  {
    path: 'admin',
    component: AdminShellComponent,
    canActivate: [roleGuard],
    data: { role: 'admin' },
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('../pages/admin/dashboard/dashboard.component').then(m => m.DashboardComponent),
        data: { title: 'Admin Dashboard' }
      },
    //   {
    //     path: 'users',
    //     loadComponent: () =>
    //       import('../pages/admin/users/users.component').then(m => m.UsersComponent),
    //     data: { title: 'Manage Users' }
    //   },
    //   {
    //     path: 'tutors',
    //     loadComponent: () =>
    //       import('../pages/admin/tutors/tutors.component').then(m => m.TutorsComponent),
    //     data: { title: 'Manage Tutors' }
    //   },
    //   {
    //     path: 'reports',
    //     loadComponent: () =>
    //       import('../pages/admin/reports/reports.component').then(m => m.ReportsComponent),
    //     data: { title: 'Reports' }
    //   },
    //   {
    //     path: 'settings',
    //     loadComponent: () =>
    //       import('../pages/admin/settings/settings.component').then(m => m.SettingsComponent),
    //     data: { title: 'System Settings' }
    //   },
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      }
    ]
  }
];
