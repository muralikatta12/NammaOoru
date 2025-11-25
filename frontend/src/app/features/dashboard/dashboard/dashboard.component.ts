// src/app/features/dashboard/dashboard/dashboard.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReportService } from '../../../core/services/report.service';
import { Report, ReportStatus } from '../../../core/models/report.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styles: [`
    .stat-card {
      background: linear-gradient(135deg, #10b981, #059669);
      transition: all 0.3s ease;
    }
    .stat-card:hover {
      transform: translateY(-10px);
      box-shadow: 0 20px 40px rgba(16,185,129,0.3);
    }
    @keyframes grow {
      from { width: 0%; }
      to { width: var(--progress); }
    }
    .btn-group .btn.active {
      background-color: #10b981 !important;
      color: white !important;
      border-color: #10b981 !important;
    }
  `]
})
export class DashboardComponent implements OnInit {
  stats = {
    total: 0,
    submitted: 0,
    inProgress: 0,
    resolved: 0,
    closed: 0
  };

  categoryStats: { name: string; count: number; color: string }[] = [];
  recentReports: Report[] = [];

  constructor(private reportService: ReportService) {}

  ngOnInit() {
    this.loadReports();
  }

  loadReports() {
    this.reportService.getReports().subscribe({
      next: (reports) => {
        this.recentReports = reports.slice(0, 6);

        this.stats.total = reports.length;
        this.stats.submitted = reports.filter(r => r.status === ReportStatus.Submitted).length;
        this.stats.inProgress = reports.filter(r => r.status === ReportStatus.InProgress).length;
        this.stats.resolved = reports.filter(r => r.status === ReportStatus.Resolved).length;
        this.stats.closed = reports.filter(r => r.status === ReportStatus.Closed).length;

        // Category stats
        const categoryMap = new Map<string, number>();
        reports.forEach(r => {
          const cat = r.category || 'Others';
          categoryMap.set(cat, (categoryMap.get(cat) || 0) + 1);
        });

        const colors = ['#10b981', '#f59e0b', '#3b82f6', '#8b5cf6', '#ef4444', '#6b7280'];
        this.categoryStats = Array.from(categoryMap.entries())
          .sort((a, b) => b[1] - a[1])
          .map(([name, count], i) => ({
            name,
            count,
            color: colors[i % colors.length]
          }));
      },
      error: () => {
        alert('Failed to load reports');
      }
    });
  }

  getProgress(status: number): number {
    if (this.stats.total === 0) return 0;
    const count = status === 0 ? this.stats.submitted :
                  status === 1 ? this.stats.inProgress :
                  status === 2 ? this.stats.resolved : this.stats.closed;
    return (count / this.stats.total) * 100;
  }
}