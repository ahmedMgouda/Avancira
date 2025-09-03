import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-auth-callback',
  template: ''
})
export class AuthCallbackComponent implements OnInit {
  constructor(
    private readonly auth: AuthService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
  this.auth.init().then(async () => {
       console.info('OAuth2 code flow login');
    // Try to parse the login response (code + state)
    const success = await this.auth.tryLoginCodeFlow();

    if (!success) {
      console.error('OAuth2 code flow login failed');
      this.router.navigateByUrl('/');
      return;
    }

    // Restore return URL from state
    const url = this.route.snapshot.queryParamMap.get('state') ?? '/';
    const isRelative = /^\/(?!\/)/.test(url) && !url.includes('://');
    this.router.navigateByUrl(isRelative ? url : '/');
  });
}

}
