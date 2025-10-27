import { Component } from '@angular/core';

import { PortalNavigationComponent } from '../portal/portal-navigation.component';

@Component({
  selector: 'app-admin-navigation',
  standalone: true,
  imports: [PortalNavigationComponent],
  templateUrl: './admin-navigation.component.html',
  styleUrls: ['./admin-navigation.component.scss']
})
export class AdminNavigationComponent {}
