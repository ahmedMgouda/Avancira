import { CommonModule } from '@angular/common';
import { Component, computed } from '@angular/core';
import { RouterModule } from '@angular/router';

import { NetworkStatusIndicatorComponent } from '@core/network';
import { ThemeToggleComponent } from '@/shared/components/theme-toggle';
import { AuthService } from '@core/auth';
import { ImageFallbackDirective } from '@/directives/image-fallback.directive';

interface PortalNavItem {
  label: string;
  link: string[];
}

type AvailableRole = 'student' | 'tutor' | 'admin';

const STUDENT_NAV: readonly PortalNavItem[] = [
  { label: 'Dashboard', link: ['/', 'student', 'dashboard'] },
  { label: 'Messages', link: ['/', 'student', 'messages'] },
  { label: 'Lessons', link: ['/', 'student', 'lessons'] },
  { label: 'Payments', link: ['/', 'student', 'payments'] },
  { label: 'Profile', link: ['/', 'student', 'profile'] },
];

const TUTOR_NAV: readonly PortalNavItem[] = [
  { label: 'Dashboard', link: ['/', 'tutor', 'dashboard'] },
  { label: 'Messages', link: ['/', 'tutor', 'messages'] },
  { label: 'Listings', link: ['/', 'tutor', 'listings'] },
  { label: 'Lessons', link: ['/', 'tutor', 'lessons'] },
  { label: 'Profile', link: ['/', 'tutor', 'profile'] },
];

const ROLE_ROUTES: Record<AvailableRole, PortalNavItem> = {
  admin: { label: 'Admin Dashboard', link: ['/', 'admin', 'dashboard'] },
  tutor: { label: 'Tutor Dashboard', link: ['/', 'tutor', 'dashboard'] },
  student: { label: 'Student Dashboard', link: ['/', 'student', 'dashboard'] },
};

@Component({
  selector: 'app-site-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ImageFallbackDirective,
    NetworkStatusIndicatorComponent,
    ThemeToggleComponent
  ],
  templateUrl: './site-header.component.html',
  styleUrls: ['./site-header.component.scss']
})
export class SiteHeaderComponent {
  readonly portalNavItems = computed((): readonly PortalNavItem[] => {
    if (!this.authService.isAuthenticated()) {
      return [];
    }

    const activeProfile = this.authService.activeProfile();

    if (activeProfile === 'student') {
      return STUDENT_NAV;
    }

    if (activeProfile === 'tutor') {
      return TUTOR_NAV;
    }

    if (this.authService.hasAdminAccess()) {
      return [ROLE_ROUTES.admin];
    }

    return [];
  });

  readonly availableRoles = computed((): readonly PortalNavItem[] => {
    if (!this.authService.isAuthenticated()) {
      return [];
    }

    const roles = new Set<AvailableRole>();

    if (this.authService.hasAdminAccess()) {
      roles.add('admin');
    }

    this.authService.roles().forEach((role) => {
      if (role === 'student' || role === 'tutor' || role === 'admin') {
        roles.add(role);
      }
    });

    return Array.from(roles).map((role) => ROLE_ROUTES[role]);
  });

  readonly dashboardLink = computed((): string[] => {
    if (!this.authService.isAuthenticated()) {
      return ['/'];
    }

    const activeProfile = this.authService.activeProfile();

    if (activeProfile === 'student') {
      return ['/', 'student', 'dashboard'];
    }

    if (activeProfile === 'tutor') {
      return ['/', 'tutor', 'dashboard'];
    }

    if (this.authService.hasAdminAccess()) {
      return ['/', 'admin', 'dashboard'];
    }

    return ['/'];
  });

  readonly profileLink = computed((): string[] => {
    if (!this.authService.isAuthenticated()) {
      return ['/'];
    }

    const activeProfile = this.authService.activeProfile();

    if (activeProfile === 'student') {
      return ['/', 'student', 'profile'];
    }

    if (activeProfile === 'tutor') {
      return ['/', 'tutor', 'profile'];
    }

    return this.dashboardLink();
  });

  constructor(public readonly authService: AuthService) {}

  startLogin(event?: Event): void {
    event?.preventDefault();
    this.authService.startLogin();
  }

  logout(): void {
    this.authService.logout();
  }
}