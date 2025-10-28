import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

import { SiteFooterComponent } from '../shared/components/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../shared/components/site-header/site-header.component';

@Component({
  selector: 'app-site-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, SiteHeaderComponent, SiteFooterComponent],
  templateUrl: './site-shell.component.html',
  styleUrls: ['./site-shell.component.scss']
})
export class SiteShellComponent {}