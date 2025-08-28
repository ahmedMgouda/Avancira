import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-signin',
  template: ''
})
export class SigninComponent implements OnInit {
  constructor(
    private readonly auth: AuthService,
    private readonly route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    const url = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/';
    const isRelative = /^\/(?!\/)/.test(url) && !url.includes('://');
    this.auth.startLogin(isRelative ? url : '/');
  }
}
