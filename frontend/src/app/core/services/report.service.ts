import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Report, CreateReportRequest } from '../models/report.model';
import { environment } from '../../../environments/environments';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private apiUrl = `${environment.apiUrl}/reports`;

  constructor(private http: HttpClient) {}

  getReports(): Observable<Report[]> {
    return this.http.get<Report[]>(this.apiUrl);
  }

  getReport(id: number): Observable<Report> {
    return this.http.get<Report>(`${this.apiUrl}/${id}`);
  }

  createReport(report: CreateReportRequest): Observable<any> {
    return this.http.post(this.apiUrl, report);
  }

  uploadPhoto(reportId: number, file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/${reportId}/photos`, formData);
  }

  updateStatus(reportId: number, status: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/${reportId}/status`, { status });
  }
}