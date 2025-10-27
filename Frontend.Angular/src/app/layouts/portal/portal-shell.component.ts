import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

import { SiteFooterComponent } from '../shared/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../shared/site-header/site-header.component';
import { PortalNavigationComponent } from './portal-navigation.component';

@Component({
  selector: 'app-portal-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, PortalNavigationComponent, SiteHeaderComponent, SiteFooterComponent],
  templateUrl: './portal-shell.component.html',
  styleUrls: ['./portal-shell.component.scss']
})
export class PortalShellComponent {}
