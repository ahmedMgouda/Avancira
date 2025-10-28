import { CommonModule } from '@angular/common';
import { Component, DestroyRef, Input, OnInit, inject } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ProfileImageComponent } from '../../components/profile-image/profile-image.component';

import { UserService } from '../../services/user.service';

import { User } from '../../models/user';
import { PortalRole } from './portal.types';

type PortalNavPath = readonly string[];

interface PortalNavItem {
  label: string;
  icon: string;
  path: PortalNavPath;
  exact?: boolean;
}

const EXACT_MATCH_OPTIONS = { exact: true } as const;
const PARTIAL_MATCH_OPTIONS = { exact: false } as const;

const NAVIGATION_ITEMS: Record<PortalRole, readonly PortalNavItem[]> = {
  student: [
    { label: 'Dashboard', icon: 'fas fa-home', path: ['dashboard'], exact: true },
    { label: 'Messages', icon: 'fas fa-comments', path: ['messages'], exact: true },
    { label: 'Lessons', icon: 'fas fa-chalkboard-teacher', path: ['lessons'], exact: true },
    { label: 'Reviews', icon: 'fas fa-star', path: ['evaluations'], exact: true },
    { label: 'Payments', icon: 'fas fa-credit-card', path: ['payments'], exact: true },
    { label: 'Invoices', icon: 'fas fa-file-invoice', path: ['invoices'], exact: true },
    { label: 'Profile', icon: 'fas fa-user-cog', path: ['profile'], exact: true },
  ],
  tutor: [
    { label: 'Dashboard', icon: 'fas fa-home', path: ['dashboard'], exact: true },
    { label: 'Messages', icon: 'fas fa-comments', path: ['messages'], exact: true },
    { label: 'Listings', icon: 'fas fa-clipboard-list', path: ['listings'], exact: true },
    { label: 'Lessons', icon: 'fas fa-chalkboard-teacher', path: ['lessons'], exact: true },
    { label: 'Reviews', icon: 'fas fa-star', path: ['evaluations'], exact: true },
    { label: 'Payments', icon: 'fas fa-credit-card', path: ['payments'], exact: true },
    { label: 'Invoices', icon: 'fas fa-file-invoice', path: ['invoices'], exact: true },
    { label: 'Sessions', icon: 'fas fa-sign-out-alt', path: ['sessions'], exact: true },
    { label: 'Profile', icon: 'fas fa-user-cog', path: ['profile'], exact: true },
    { label: 'Table Test', icon: 'fas fa-table', path: ['tabletest'], exact: true },
  ],
};

@Component({
  selector: 'app-portal-navigation',
  standalone: true,
  imports: [CommonModule, RouterModule, ProfileImageComponent],
  templateUrl: './portal-navigation.component.html',
  styleUrls: ['./portal-navigation.component.scss']
})
export class PortalNavigationComponent implements OnInit {
  @Input()
  set role(value: PortalRole | null) {
    if (value && this.roleInternal !== value) {
      this.roleInternal = value;
      this.navItems = [...NAVIGATION_ITEMS[value]];
    }
  }

  get role(): PortalRole | null {
    return this.roleInternal;
  }

  currentPage = 'Dashboard';
  user: User | null = null;
  navItems: readonly PortalNavItem[] = NAVIGATION_ITEMS.student;

  private roleInternal: PortalRole = 'student';
  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private readonly userService: UserService,
    private readonly router: Router,
    private readonly activatedRoute: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => this.updateCurrentPage());

    this.userService.user$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((userData) => {
        this.user = userData;
      });

    this.userService
      .getUser()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: (err) => console.error('Failed to load user data:', err)
      });

    this.updateCurrentPage();
  }

  protected buildLink(path: PortalNavPath): any[] {
    return ['/', this.roleInternal, ...path];
  }

  protected linkActiveOptions(item: PortalNavItem) {
    return item.exact ? EXACT_MATCH_OPTIONS : PARTIAL_MATCH_OPTIONS;
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
