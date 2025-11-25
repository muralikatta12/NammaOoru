// src/app/features/reports/report-list/report-list.component.ts
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportService } from '../../../core/services/report.service';
import { ReportCardComponent } from '../components/report-card/report-card.component';
import { Report } from '../../../core/models/report.model';
import { AuthService } from '../../../core/services/auth.service';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-report-list',
  standalone: true,
  imports: [CommonModule, ReportCardComponent, RouterLink],
  templateUrl: './report-list.component.html',
})
export class ReportListComponent implements OnInit {
  reports: Report[] = [];
  loading = true;
  error = false;

  constructor(
    public authService: AuthService,
    private reportService: ReportService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadReports();
  }

  loadReports() {
    this.loading = true;
    this.error = false;

    this.reportService.getReports().subscribe({
      next: (reports) => {
        this.reports = reports;
        this.loading = false;
        console.log('All reports loaded:', reports.length);
        this.cdr.detectChanges();

      },
      error: (err) => {
        console.error('Failed to load reports:', err);
        this.error = true;
        this.loading = false;
        alert('Failed to load reports. Please try again.');
      }
    });
  }
}