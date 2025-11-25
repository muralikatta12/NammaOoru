import { Component, OnInit } from '@angular/core';
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

  constructor(
    public authService: AuthService,
    private reportService: ReportService
  ) {}

  ngOnInit() {
    this.reportService.getReports().subscribe({
      next: (data) => {
        this.reports = data;
        this.loading = false;
      },
      error: () => {
        alert('Failed to load reports');
        this.loading = false;
      }
    });
  }
}