import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterModule } from '@angular/router';

import { SiteHeaderComponent } from '../shared/components/site-header/site-header.component';
import { PortalNavigationComponent } from './portal-navigation.component';
import { LayoutContextService } from '../shared/services/layout-context.service';

@Component({
  selector: 'app-portal-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, PortalNavigationComponent, SiteHeaderComponent],
  templateUrl: './portal-shell.component.html',
  styleUrls: ['./portal-shell.component.scss']
})
export class PortalShellComponent {
  protected readonly layoutContext = inject(LayoutContextService);
}