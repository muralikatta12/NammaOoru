// src/app/features/reports/report-detail/report-detail.component.ts
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ReportService } from '../../../core/services/report.service';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environments';

@Component({
  selector: 'app-report-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './report-detail.component.html',
  styleUrls: ['./report-detail.component.css']
})
export class ReportDetailComponent implements OnInit {
  report: any = null;
  photos: any[] = [];
  loading = true;
  baseUrl = environment.apiUrl;

  // Photo Popup
  showPhotoPopup = false;
  popupPhoto: any = null;
  popupIndex = 0;

  statusBadge = ['bg-warning text-dark', 'bg-info text-white', 'bg-success text-white', 'bg-secondary text-white'];
  statusLabels = ['Submitted', 'In Progress', 'Resolved', 'Closed'];

  get isAdmin(): boolean {
    const user = this.authService.getCurrentUser();
    return user?.role === 'Admin' || user?.role === 'Official';
  }

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private reportService: ReportService,
    public authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id || isNaN(id)) {
      this.router.navigate(['/reports']);
      return;
    }
    this.loadReport(id);
  }

  loadReport(id: number) {
    this.loading = true;
    this.reportService.getReportById(id).subscribe({
      next: (report: any) => {
        this.report = report;
        this.photos = report.photos || [];
        this.loading = false;
          this.cdr.detectChanges();

      },
      error: () => {
        alert('Report not found');
        this.router.navigate(['/reports']);
      }
    });
  }

  // POPUP FUNCTIONS
  openPhotoPopup(photo: any, index: number) {
    this.popupPhoto = photo;
    this.popupIndex = index;
    this.showPhotoPopup = true;
  }

  closePopup() {
    this.showPhotoPopup = false;
  }

  prevPhoto() {
    if (this.popupIndex > 0) {
      this.popupIndex--;
      this.popupPhoto = this.photos[this.popupIndex];
    }
  }

  nextPhoto() {
    if (this.popupIndex < this.photos.length - 1) {
      this.popupIndex++;
      this.popupPhoto = this.photos[this.popupIndex];
    }
  }

  updateStatus(status: number) {
    if (!confirm(`Mark as ${this.statusLabels[status]}?`)) return;
    const old = this.report.status;
    this.report.status = status;

    this.reportService.updateStatus(this.report.id, status).subscribe({
      next: () => alert('Status updated!'),
      error: () => {
        this.report.status = old;
        alert('Failed');
      }
    });
  }

  goBack() {
    this.router.navigate(['/reports']);
  }
}