import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-navigation-bar',
  imports: [RouterModule, CommonModule, FormsModule],
  templateUrl: './navigation-bar.component.html',
  styleUrl: './navigation-bar.component.scss'
})
export class NavigationBarComponent {
  navLinks = [
    { label: 'Dashboard', path: '/dashboard' },
    { label: 'My Messages', path: '/messages' },
    { label: 'My Listings', path: '/listings' },
    { label: 'Evaluations', path: '/evaluations' },
    { label: 'My Account', path: '/profile' },
    { label: 'Premium', path: '/premium' },
  ];

  constructor(private router: Router) {}

  isActive(path: string): boolean {
    return this.router.url === path;
  }
}
