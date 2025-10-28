import { Routes } from '@angular/router';

import { SiteShellComponent } from '../layouts/site/site-shell.component';

export const publicRoutes: Routes = [
  {
    path: '',
    component: SiteShellComponent,
    children: [
      {
        path: '',
        loadComponent: () =>
          import('../pages/home/home.component').then(m => m.HomeComponent),
        data: { title: 'Home' }
      },
      {
        path: 'about',
        loadComponent: () =>
          import('../pages/about/about.component').then(m => m.AboutComponent),
        data: { title: 'About Us' }
      }
    ]
  }
];
