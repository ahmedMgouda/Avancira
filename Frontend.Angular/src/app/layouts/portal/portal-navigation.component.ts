import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter, Subscription } from 'rxjs';

import { ProfileImageComponent } from '../../components/profile-image/profile-image.component';

import { UserService } from '../../services/user.service';

import { User } from '../../models/user';

@Component({
  selector: 'app-portal-navigation',
  standalone: true,
  imports: [CommonModule, RouterModule, ProfileImageComponent],
  templateUrl: './portal-navigation.component.html',
  styleUrls: ['./portal-navigation.component.scss']
})
export class PortalNavigationComponent implements OnInit, OnDestroy {
  currentPage = 'Home';
  user!: User;
  private userSub?: Subscription;

  constructor(
    private readonly userService: UserService,
    private readonly router: Router,
    private readonly activatedRoute: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => {
        let route = this.activatedRoute.firstChild;

        while (route?.firstChild) {
          route = route.firstChild;
        }

        const title = route?.snapshot.data['title'];
        if (title) {
          this.currentPage = title;
        }
      });

    this.userSub = this.userService.user$.subscribe((userData) => {
      if (userData) {
        this.user = userData;
      }
    });

    this.userService.getUser().subscribe({
      error: (err) => console.error('Failed to load user data:', err)
    });
  }

  ngOnDestroy(): void {
    this.userSub?.unsubscribe();
  }
}
