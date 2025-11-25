import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './components/layout/header/header';
import { DarkModeToggleComponent } from './components/ui/dark-mode-toggle/dark-mode-toggle';
import { DashboardComponent } from './features/dashboard/dashboard/dashboard.component';
import { ReportDetailComponent } from './features/reports/report-detail/report-detail.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, DarkModeToggleComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class AppComponent {
  title = 'NammaOoru';
}