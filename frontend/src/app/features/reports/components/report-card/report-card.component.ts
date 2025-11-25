import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { environment } from '../../../../../environments/environments';
import { ReportStatus, StatusBadge } from '../../../../core/models/report.model';
import { Report } from '../../../../core/models/report.model';
@Component({
  selector: 'app-report-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './report-card.component.html',
  styles: [`
    .hover-shadow:hover {
      transform: translateY(-8px);
      box-shadow: 0 20px 40px rgba(0,0,0,0.15) !important;
    }
  `]
})
export class ReportCardComponent {
  @Input() report!: Report;

  baseUrl = environment.apiUrl;
  
  statusBadge = StatusBadge;
  
  statusLabels: Record<ReportStatus, string> = {
    [ReportStatus.Submitted]: 'Submitted',
    [ReportStatus.InProgress]: 'In Progress',
    [ReportStatus.Resolved]: 'Resolved',
    [ReportStatus.Closed]: 'Closed'
  };

  get primaryPhoto() {
    return this.report.photos?.find(p => p.isPrimary) || this.report.photos?.[0];
  }
}