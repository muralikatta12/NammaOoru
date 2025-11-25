// src/app/app.routes.ts
import { Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';

export const appRoutes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },

  // Dashboard
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [AuthGuard]
  },

  // Reports
  {
    path: 'reports',
    loadComponent: () => import('./features/reports/report-list/report-list.component').then(m => m.ReportListComponent),
    canActivate: [AuthGuard]
  },
  {
    path: 'reports/new',
    loadComponent: () => import('./features/reports/create-report/create-report.component').then(m => m.CreateReportComponent),
    canActivate: [AuthGuard]
  },
  {
    path: 'reports/:id',
    loadComponent: () => import('./features/reports/report-detail/report-detail.component').then(m => m.ReportDetailComponent),
    canActivate: [AuthGuard]
  },

  // Auth
  {
    path: 'auth/login',
    loadComponent: () => import('./features/auth/login/login').then(m => m.LoginComponent)
  },
  {
    path: 'auth/register',
    loadComponent: () => import('./features/auth/register/register').then(m => m.RegisterComponent)
  },
  {
    path: 'auth/verify',
    loadComponent: () => import('./features/auth/verify-otp/verify-otp').then(m => m.VerifyOtpComponent)
  },

  { path: '**', redirectTo: '/dashboard' }
];