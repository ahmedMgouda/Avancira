import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

import { PortalNavigationComponent } from '../portal/portal-navigation.component';
import { SiteHeaderComponent } from '../shared/components/site-header/site-header.component';

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, PortalNavigationComponent, SiteHeaderComponent],
  templateUrl: './admin-shell.component.html',
  styleUrls: ['./admin-shell.component.scss']
})
export class AdminShellComponent {}