import { computed, Injectable, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter, map } from 'rxjs';

import { PortalRole } from '../../portal/portal.types';

export interface LayoutState {
  sidebarCollapsed: boolean;
  isMobile: boolean;
  theme: 'light' | 'dark';
}

@Injectable({
  providedIn: 'root'
})
export class LayoutContextService {
  private readonly layoutState = signal<LayoutState>({
    sidebarCollapsed: false,
    isMobile: false,
    theme: 'light'
  });

  // define the signal but initialize later
  readonly currentRole = signal<PortalRole | null>(null);

  // Layout state signals
  readonly sidebarCollapsed = computed(() => this.layoutState().sidebarCollapsed);
  readonly isMobile = computed(() => this.layoutState().isMobile);
  readonly theme = computed(() => this.layoutState().theme);

  constructor(
    private readonly router: Router,
    private readonly activatedRoute: ActivatedRoute
  ) {
    this.initMobileDetection();
    this.initCurrentRoleTracking();
  }

  private initCurrentRoleTracking(): void {
    const currentRoleSignal = toSignal(
      this.router.events.pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        map(() => this.extractRoleFromRoute())
      ),
      { initialValue: this.extractRoleFromRoute() }
    );

    // sync the signal to our property
    this.currentRole.set(currentRoleSignal());
    // react to changes
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd)
    ).subscribe(() => this.currentRole.set(this.extractRoleFromRoute()));
  }

  toggleSidebar(): void {
    this.layoutState.update(state => ({
      ...state,
      sidebarCollapsed: !state.sidebarCollapsed
    }));
  }

  setSidebarCollapsed(collapsed: boolean): void {
    this.layoutState.update(state => ({
      ...state,
      sidebarCollapsed: collapsed
    }));
  }

  setTheme(theme: 'light' | 'dark'): void {
    this.layoutState.update(state => ({
      ...state,
      theme
    }));
  }

  isRole(role: PortalRole): boolean {
    return this.currentRole() === role;
  }

  private extractRoleFromRoute(): PortalRole | null {
    let route = this.activatedRoute;
    while (route) {
      if (route.snapshot.data['role']) {
        return route.snapshot.data['role'] as PortalRole;
      }
      route = route.firstChild!;
    }
    return null;
  }

  private initMobileDetection(): void {
    const checkMobile = () => {
      this.layoutState.update(state => ({
        ...state,
        isMobile: window.innerWidth < 768
      }));
    };

    checkMobile();
    window.addEventListener('resize', checkMobile);
  }
}
