import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-auth-callback',
  template: '<p>Signing you in...</p>'
})
export class AuthCallbackComponent implements OnInit {
  constructor(
    private readonly auth: AuthService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
  ) {}

  async ngOnInit(): Promise<void> {
    if (!this.auth.isAuthenticated()) {
      console.error('❌ OAuth2 code flow login failed');
      this.router.navigateByUrl('/');
      return;
    }

    // Restore returnUrl from state
    const url = this.route.snapshot.queryParamMap.get('state') ?? '/';
    const isRelative = /^\/(?!\/)/.test(url) && !url.includes('://');

    console.info('✅ OAuth2 login successful, redirecting to:', url);
    this.router.navigateByUrl(isRelative ? url : '/');
  }
}
