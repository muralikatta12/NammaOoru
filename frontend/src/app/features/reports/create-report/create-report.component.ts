// src/app/features/reports/create-report/create-report.component.ts
import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ReportService } from '../../../core/services/report.service';
import { AuthService } from '../../../core/services/auth.service';
import { CommonModule } from '@angular/common';
import { lastValueFrom } from 'rxjs';

@Component({
  selector: 'app-create-report',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './create-report.component.html',
})
export class CreateReportComponent {
  loading = false;
  photos: File[] = [];
  photoPreviews: string[] = [];

  private fb = new FormBuilder();

  form = this.fb.group({
    title: ['', Validators.required],
    description: [''],
    category: ['Pothole', Validators.required],
    locationAddress: ['', Validators.required]
  });

  constructor(
    private reportService: ReportService,
    private authService: AuthService,
    public router: Router
  ) {}

  onFileSelect(event: any) {
    const files: FileList = event.target.files;
    for (let i = 0; i < files.length; i++) {
      const file = files[i];
      if (file.type.match(/image.*/)) {
        this.photos.push(file);
        const reader = new FileReader();
        reader.onload = (e: any) => this.photoPreviews.push(e.target.result);
        reader.readAsDataURL(file);
      }
    }
  }

  removePhoto(index: number) {
    this.photos.splice(index, 1);
    this.photoPreviews.splice(index, 1);
  }

  async onSubmit() {
    if (this.form.invalid || this.loading) return;
    this.loading = true;

    try {
      // EXACTLY WHAT YOUR .NET BACKEND EXPECTS (camelCase + attachments)
      const reportData = {
        title: this.form.value.title!.trim(),
        description: this.form.value.description?.trim() || '',
        category: this.form.value.category!,
        locationAddress: this.form.value.locationAddress!.trim(),
        attachments: []  // Required by your backend
      };

      console.log('Sending report:', reportData);

      // Use modern lastValueFrom instead of deprecated toPromise()
      const createResponse: any = await lastValueFrom(
        this.reportService.createReport(reportData)
      );

      // Your backend returns: { id: 123 }
      const reportId = createResponse?.id;

      if (!reportId) {
        throw new Error('Report created but no ID received from server');
      }

      // Upload photos one by one
      for (const file of this.photos) {
        await lastValueFrom(this.reportService.uploadPhoto(reportId, file));
      }

      alert('Report submitted successfully!');
      this.router.navigate(['/reports']);  // Go to reports list

    } catch (err: any) {
      console.error('Report submission failed:', err);
      const errorMsg = err?.error?.message || err?.message || 'Unknown error';
      alert('Failed to submit report: ' + errorMsg);
    } finally {
      this.loading = false;
    }
  }
}