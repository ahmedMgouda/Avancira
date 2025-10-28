import { Routes } from '@angular/router';

//import { BlankShellComponent } from '../layouts/blank/blank-shell.component';
import { adminRoutes } from './app.routes.admin';
import { publicRoutes } from './app.routes.public';
import { studentRoutes } from './app.routes.student';
import { tutorRoutes } from './app.routes.tutor';

export const routes: Routes = [
  ...publicRoutes,
  ...studentRoutes,
  ...tutorRoutes,
  ...adminRoutes,
  // {
  //   // path: '404',
  //   // component: BlankShellComponent,
  //   // children: [
  //   //   {
  //   //     path: '',
  //   //     loadComponent: () =>
  //   //       import('../pages/errors/not-found/not-found.component').then(m => m.NotFoundComponent),
  //   //     data: { title: 'Page Not Found' }
  //   //   }
  //   // ]
  // },
  // {
  //   path: '**',
  //   redirectTo: ''
  // }
];