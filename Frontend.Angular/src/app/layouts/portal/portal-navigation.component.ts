import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter } from 'rxjs';

import { ProfileImageComponent } from '../../components/profile-image/profile-image.component';

import { UserService } from '../../services/user.service';
import { LayoutContextService } from '../shared/services/layout-context.service';

import { User } from '../../models/user';
import { ADMIN_NAV_ITEMS } from '../admin/config/admin-nav.config';
import { buildRoleLink, LINK_ACTIVE_OPTIONS } from '../shared/utils/layout.utils';
import { STUDENT_NAV_ITEMS } from './config/student-nav.config';
import { TUTOR_NAV_ITEMS } from './config/tutor-nav.config';
import { NavigationConfig, PortalNavItem } from './portal.types';

const NAVIGATION_CONFIG: NavigationConfig = {
  student: STUDENT_NAV_ITEMS,
  tutor: TUTOR_NAV_ITEMS,
  admin: ADMIN_NAV_ITEMS
};

@Component({
  selector: 'app-portal-navigation',
  standalone: true,
  imports: [CommonModule, RouterModule, ProfileImageComponent],
  templateUrl: './portal-navigation.component.html',
  styleUrls: ['./portal-navigation.component.scss']
})
export class PortalNavigationComponent implements OnInit {
  currentPage = 'Dashboard';
  user: User | null = null;
  navItems: readonly PortalNavItem[] = [];

  private readonly layoutContext = inject(LayoutContextService);
  private readonly userService = inject(UserService);
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    // Subscribe to role changes and update navigation
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.updateNavigation();
        this.updateCurrentPage();
      });

    // Subscribe to user data
    this.userService.user$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((userData) => {
        this.user = userData;
      });

    // Load user data
    this.userService
      .getUser()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: (err) => console.error('Failed to load user data:', err)
      });

    this.updateNavigation();
    this.updateCurrentPage();
  }

  protected buildLink(path: readonly string[]): any[] {
    return buildRoleLink(this.layoutContext.currentRole(), path);
  }

  protected linkActiveOptions(item: PortalNavItem) {
    return item.exact ? LINK_ACTIVE_OPTIONS.exact : LINK_ACTIVE_OPTIONS.partial;
  }

  private updateNavigation(): void {
    const role = this.layoutContext.currentRole();
    if (role) {
      this.navItems = [...NAVIGATION_CONFIG[role]];
    }
  }

  private updateCurrentPage(): void {
    let route = this.activatedRoute.firstChild;

    while (route?.firstChild) {
      route = route.firstChild;
    }

    const title = route?.snapshot.data['title'];
    if (title) {
      this.currentPage = title;
    }
  }
}
