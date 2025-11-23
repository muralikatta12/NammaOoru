import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';


@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {
    constructor(private auth: AuthService, private router: Router) {}

    canActivate(route: ActivatedRouteSnapshot): boolean | UrlTree {
        const required = route.data['roles'];
        if (!this.auth.isLoggedIn()) {
            return this.router.createUrlTree(['/auth/login']);
        }

        const userRole = this.auth.getUserRole?.() ?? null;

        if (!userRole) {
            return this.router.createUrlTree(['/auth/login']);
        }

        const allowed = Array.isArray(required) ? required : [required];

        if (allowed.includes(userRole)) {
            return true;
        }
        return this.router.createUrlTree(['/forbidden']);
    }
}