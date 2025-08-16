import { Injectable } from '@angular/core';
import {
  ActivatedRouteSnapshot,
  CanActivate,
  Router,
  RouterStateSnapshot,
  UrlTree
} from '@angular/router';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(
    _route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean | UrlTree> {
    const evaluate = (): boolean | UrlTree => {
      if (this.authService.isAuthenticated()) {
        return true;
      }

      // No valid session → redirect to login and remember where we came from
      return this.router.createUrlTree(['/signin'], {
        queryParams: { returnUrl: state.url }
      });
    };

    if (this.authService.refreshing) {
      return this.authService.waitForRefresh().pipe(map(() => evaluate()));
    }

    return of(evaluate());
  }
}
