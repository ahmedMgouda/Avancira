import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';

import { AuthService } from '../../../services/auth.service';
import { UserService } from '../../../services/user.service';

import { ImageFallbackDirective } from '../../../directives/image-fallback.directive';

import { User } from '../../../models/user';

@Component({
  selector: 'app-header',
  imports: [CommonModule, RouterModule, ImageFallbackDirective],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})

export class HeaderComponent implements OnInit, OnDestroy {
  isMenuOpen = false;
  roles: string[] = [];
  currentRole: 'student' | 'tutor' = 'student';
  user!: User;
  private userSub?: Subscription;


  constructor(
    private userService: UserService,
    private authService: AuthService
  ) { }


  ngOnInit(): void {
    if (this.isLoggedIn()) {
      this.userSub = this.userService.user$.subscribe(user => {
        if (user) this.user = user;
      });
      this.fetchUserInfo();
    }
    // this.roles = this.authService.getRoles();

    // const savedRole = this.authService.getCurrentRole();
    // if (savedRole && this.roles.includes(savedRole)) {
    //   this.currentRole = savedRole as 'student' | 'tutor';
    // } else if (this.roles.length > 0) {
    //   this.currentRole = this.roles[0] as 'student' | 'tutor';
    //   // this.authService.saveCurrentRole(this.currentRole);
    // }
  }

  ngOnDestroy(): void {
    this.userSub?.unsubscribe();
  }

  switchRole(role: 'student' | 'tutor'): void {
    if (this.currentRole !== role) {
      this.currentRole = role;
      // this.authService.saveCurrentRole(role);
    }
  }

  toggleMenu() {
    this.isMenuOpen = !this.isMenuOpen;
  }

  hasMultipleRoles(): boolean {
    return this.roles.length > 1;
  }

  isLoggedIn(): boolean {
    return this.authService.isAuthenticated();
  }

  logout() {
    this.authService.logout();
  }

  fetchUserInfo() {
    this.userService.getUser().subscribe({
      next: (userData) => (this.user = userData),
      error: (err) => console.error('Failed to load user data:', err),
    });
  }

  startLogin(event?: Event): void {
    event?.preventDefault();
    this.authService.startLogin();
  }
}