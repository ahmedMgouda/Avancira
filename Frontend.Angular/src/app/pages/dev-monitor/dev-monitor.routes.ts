import { Routes } from '@angular/router';

import { DevMonitorComponent } from './dev-monitor.component';

import { environment } from '../../environments/environment';

export const DEV_MONITOR_ROUTES: Routes = [
  {
    path: '',
    component: DevMonitorComponent,
    canActivate: [() => !environment.production] // only allow in dev
  }
];
