import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';

import { SiteFooterComponent } from '../shared/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../shared/site-header/site-header.component';
import { PortalNavigationComponent } from './portal-navigation.component';
import { PortalRole } from './portal.types';

@Component({
  selector: 'app-portal-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, PortalNavigationComponent, SiteHeaderComponent, SiteFooterComponent],
  templateUrl: './portal-shell.component.html',
  styleUrls: ['./portal-shell.component.scss']
})
export class PortalShellComponent {
  private readonly route = inject(ActivatedRoute);

  protected readonly role = toSignal(
    this.route.data.pipe(map((data) => (data['role'] as PortalRole | undefined) ?? null)),
    { initialValue: (this.route.snapshot.data['role'] as PortalRole | undefined) ?? null }
  );
}
