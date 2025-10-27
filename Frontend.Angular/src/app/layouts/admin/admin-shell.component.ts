import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

import { AdminNavigationComponent } from './admin-navigation.component';
import { SiteFooterComponent } from '../shared/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../shared/site-header/site-header.component';

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, AdminNavigationComponent, SiteHeaderComponent, SiteFooterComponent],
  templateUrl: './admin-shell.component.html',
  styleUrls: ['./admin-shell.component.scss']
})
export class AdminShellComponent {}
