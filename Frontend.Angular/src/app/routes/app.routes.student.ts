import { Routes } from '@angular/router';

import { PortalShellComponent } from '../layouts/portal/portal-shell.component';

import { roleGuard } from '../guards/role.guard';

export const studentRoutes: Routes = [
  {
    path: 'student',
    component: PortalShellComponent,
    canActivate: [roleGuard],
    data: { role: 'student', requiredRole: 'student' },
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('../pages/student/dashboard/dashboard.component').then(m => m.DashboardComponent),
        data: { title: 'Dashboard' }
      },
      {
        path: 'messages',
        loadComponent: () =>
          import('../pages/messages/messages.component').then(m => m.MessagesComponent),
        data: { title: 'Messages' }
      },
      {
        path: 'lessons',
        loadComponent: () =>
          import('../pages/student/lessons/lessons.component').then(m => m.LessonsComponent),
        data: { title: 'Lessons' }
      },
      {
        path: 'evaluations',
        loadComponent: () =>
          import('../pages/evaluations/evaluations.component').then(m => m.EvaluationsComponent),
        data: { title: 'Reviews' }
      },
      {
        path: 'payments',
        loadComponent: () =>
          import('../pages/payments/payments.component').then(m => m.PaymentsComponent),
        data: { title: 'Payments' }
      },
      {
        path: 'invoices',
        loadComponent: () =>
          import('../pages/invoices/invoices.component').then(m => m.InvoicesComponent),
        data: { title: 'Invoices' }
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('../pages/profile/profile.component').then(m => m.ProfileComponent),
        data: { title: 'Profile Settings' }
      },
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      }
    ]
  }
];