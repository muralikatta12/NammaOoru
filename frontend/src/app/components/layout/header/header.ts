// src/app/components/layout/header/header.component.ts
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './header.html',  // FIXED: .component.html
  styles: [`
    .navbar-brand {
      font-weight: 900 !important;
      letter-spacing: 1px;
    }
  `]
})
export class HeaderComponent {
  constructor(
    public authService: AuthService,
    private router: Router
  ) {}

  // THIS IS THE CORRECT WAY — CHECK isVerified FIELD
  get isVerified(): boolean {
    return this.authService.getCurrentUser()?.isVerified === true;
  }

  get userName(): string {
    return this.authService.getCurrentUser()?.firstName || 'Citizen';
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }
}