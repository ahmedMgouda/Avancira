import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router, RouterModule } from '@angular/router';

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
export class HeaderComponent {
  isMenuOpen = false;
  roles: string[] = [];
  currentRole: 'student' | 'tutor' = 'student';
  user!: User;


  constructor(
    private router: Router,
    private userService: UserService,
    private authService: AuthService
  ) { }


  ngOnInit(): void {
    if (this.isLoggedIn()) {
      this.fetchUserInfo();
    }
    this.roles = this.authService.getRoles();

    const savedRole = this.authService.getCurrentRole();
    if (savedRole && this.roles.includes(savedRole)) {
      this.currentRole = savedRole as 'student' | 'tutor';
    } else if (this.roles.length > 0) {
      this.currentRole = this.roles[0] as 'student' | 'tutor';
      this.authService.saveCurrentRole(this.currentRole);
    }
  }

  switchRole(role: 'student' | 'tutor'): void {
    if (this.currentRole !== role) {
      this.currentRole = role;
      this.authService.saveCurrentRole(role);
    }
  }

  toggleMenu() {
    this.isMenuOpen = !this.isMenuOpen;
  }

  hasMultipleRoles(): boolean {
    return this.roles.length > 1;
  }

  isLoggedIn(): boolean {
    return this.authService.isLoggedIn();
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/']);
  }

  fetchUserInfo() {
    this.userService.getUser().subscribe({
      next: (userData) => (this.user = userData),
      error: (err) => console.error('Failed to load user data:', err),
    });
  }
}