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
  templateUrl: './header.html',  // ← MUST BE .component.html
  styles: [`
    .navbar-brand {
      font-weight: 900;
      letter-spacing: 1px;
    }
    .btn-sm {
      font-size: 0.85rem;
    }
  `]
})
export class HeaderComponent {
  constructor(
    public authService: AuthService, 
    private router: Router
  ) {}

  logout() {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }
}