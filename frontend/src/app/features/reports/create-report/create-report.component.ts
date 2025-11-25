import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, FormGroup } from '@angular/forms';
import { ReportService } from '../../../core/services/report.service';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { CreateReportRequest } from '../../../core/models/report.model';

@Component({
  selector: 'app-create-report',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './create-report.component.html',
})
export class CreateReportComponent {
  form: FormGroup;

  files: File[] = [];
  uploading = false;

  constructor(
    private fb: FormBuilder,
    private reportService: ReportService,
    private router: Router
  ) {
    this.form = this.fb.group({
      title: ['', Validators.required],
      description: ['', Validators.required],
      category: ['', Validators.required],
      locationAddress: ['', Validators.required],
    });
  }

  onFilesSelected(event: any) {
    this.files = Array.from(event.target.files);
  }

  submit() {
    if (this.form.invalid) return;

    this.uploading = true;
    const data = this.form.value as CreateReportRequest;

    this.reportService.createReport(data).subscribe({
      next: (response: any) => {
        const reportId = response.id;

        if (this.files.length > 0) {
          const uploads = this.files.map(file =>
            this.reportService.uploadPhoto(reportId, file).toPromise()
          );
          Promise.all(uploads).then(() => {
            this.router.navigate(['/reports']);
          });
        } else {
          this.router.navigate(['/reports']);
        }
      },
      error: () => {
        alert('Failed to create report');
        this.uploading = false;
      }
    });
  }
}