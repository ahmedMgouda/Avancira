import { CommonModule } from '@angular/common';
import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter, Subscription } from 'rxjs';

import { ProfileImageComponent } from '../../../components/profile-image/profile-image.component';

import { UserService } from '../../../services/user.service';

import { User } from '../../../models/user';

@Component({
  selector: 'app-sidebar',
  imports: [CommonModule, RouterModule, ProfileImageComponent],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent implements OnInit, OnDestroy {
  currentPage: string = 'Home'; // Default breadcrumb title
  user!: User;
  private userSub?: Subscription;

  constructor(
    private userService: UserService,
    private router: Router,
    private activatedRoute: ActivatedRoute
  ) { }


  ngOnInit() {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      let route = this.activatedRoute.firstChild;
      console.log('First Child Route:', route); // Debugging log

      while (route?.firstChild) {
        route = route.firstChild;
        console.log('Navigating deeper:', route);
      }

      if (route?.snapshot.data['title']) {
        this.currentPage = route.snapshot.data['title']; // Get title from route data
        console.log('Updated Current Page:', this.currentPage); // Debugging log
      }
    });

    this.userSub = this.userService.user$.subscribe(userData => {
      if (userData) this.user = userData;
    });
    this.userService.getUser().subscribe({
      error: (err) => console.error('Failed to load user data:', err),
    });
  }

  ngOnDestroy(): void {
    this.userSub?.unsubscribe();
  }

}
