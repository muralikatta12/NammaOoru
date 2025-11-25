import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ReportService } from '../../../core/services/report.service';
import { CommonModule } from '@angular/common';
import { Report, ReportPhoto, StatusBadge, statusLabels } from '../../../core/models/report.model';
import { environment } from '../../../../environments/environments';

@Component({
  selector: 'app-report-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './report-detail.component.html',
})
export class ReportDetailComponent implements OnInit {
  report: Report = {
    id: 0,
    title: '',
    description: '',
    category: '',
    locationAddress: '',
    status: 0,
    priority: 0,
    upvoteCount: 0,
    createdAt: new Date().toISOString(),
    createdByUserId: 0,
    photos: []
  } as Report;

  loading = true;
  baseUrl = environment.apiUrl;
  statusBadge = StatusBadge;
  statusLabels = statusLabels;
  primaryPhoto: ReportPhoto | null = null;

  constructor(private route: ActivatedRoute, private reportService: ReportService) {}

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.loading = false;
      return;
    }

    this.reportService.getReport(id).subscribe({
      next: (r) => {
        this.report = r;
        // determine primary photo (explicit primary or first photo)
        this.primaryPhoto = r.photos.find(p => p.isPrimary) || (r.photos.length ? r.photos[0] : null);
        this.loading = false;
      },
      error: () => {
        alert('Failed to load report');
        this.loading = false;
      }
    });
  }
}