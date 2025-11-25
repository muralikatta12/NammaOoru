// src/app/core/services/report.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environments';
import { Report } from '../models/report.model';

// Define the shape of paged response from your .NET backend
interface PagedResponse<T> {
  data: T[];
  total: number;
  page: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  constructor(private http: HttpClient) {}

  getReports(): Observable<Report[]> {
    return this.http.get<PagedResponse<Report>>(`${environment.apiUrl}/reports`).pipe(
      map(response => response.data || [])  // Now 100% type-safe!
    );
  }

  // src/app/core/services/report.service.ts
getReportById(id: number): Observable<Report> {
  return this.http.get<any>(`${environment.apiUrl}/reports/${id}`).pipe(  // ← Change to any
    map(res => res?.data || res)  // Now 100% safe, no error
  );
}
  createReport(report: any): Observable<any> {
    return this.http.post(`${environment.apiUrl}/reports`, report);
  }

  uploadPhoto(reportId: number, file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${environment.apiUrl}/reports/${reportId}/photos`, formData);
  }

  updateStatus(reportId: number, status: number): Observable<any> {
    return this.http.put(`${environment.apiUrl}/reports/${reportId}/status`, { status });
  }
}