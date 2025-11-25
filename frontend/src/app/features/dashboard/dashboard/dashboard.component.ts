// src/app/features/dashboard/dashboard.component.ts
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportService } from '../../../core/services/report.service';
import { AuthService } from '../../../core/services/auth.service';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  stats = {
    total: 0,
    submitted: 0,
    inProgress: 0,
    resolved: 0
  };

  categoryData: { name: string; value: number }[] = [];
  recentReports: any[] = [];

  // ADD THIS — FIXES THE ERROR
  statusLabels = ['Submitted', 'In Progress', 'Resolved', 'Closed'];

  constructor(
    private reportService: ReportService,
    public authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData() {
    this.reportService.getReports().subscribe({
      next: (reports: any[]) => {
        this.stats.total = reports.length;
        this.stats.submitted = reports.filter(r => r.status === 0).length;
        this.stats.inProgress = reports.filter(r => r.status === 1).length;
        this.stats.resolved = reports.filter(r => r.status === 2).length;

        // Category Data
        const map = new Map<string, number>();
        reports.forEach(r => {
          const cat = r.category || 'Others';
          map.set(cat, (map.get(cat) || 0) + 1);
        });
        this.categoryData = Array.from(map, ([name, value]) => ({ name, value }));

        // Recent Reports
        this.recentReports = reports
          .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
          .slice(0, 7);
        this.cdr.detectChanges();
      }
    });
  }

  get successRate(): string {
    return this.stats.total > 0 
      ? ((this.stats.resolved / this.stats.total) * 100).toFixed(1) 
      : '0';
  }
}