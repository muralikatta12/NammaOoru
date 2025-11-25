import { Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';

export const appRoutes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  
  // DASHBOARD — CORRECT PATH!
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [AuthGuard]
  },

  // HOME → DASHBOARD
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
{
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [AuthGuard]
  },

  // REPORTS
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
  // PUBLIC ROUTES (NO GUARD)
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

  // PROTECTED ROUTES
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

  // Fallback
  { path: '**', redirectTo: '/dashboard' }
];