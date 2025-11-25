import { Component, Renderer2, Inject } from '@angular/core';
import { DOCUMENT, CommonModule } from '@angular/common';

@Component({
  selector: 'app-dark-mode-toggle',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button 
      (click)="toggle()"
      class="position-fixed bottom-0 end-0 m-4 btn btn-lg rounded-circle shadow-lg z-1050"
      style="width: 60px; height: 60px; background: rgba(15,23,42,0.9); backdrop-filter: blur(10px); border: 2px solid rgba(255,255,255,0.1);">
      <i class="bi bi-moon-stars-fill text-white" *ngIf="!isDark"></i>
      <i class="bi bi-sun-fill text-warning" *ngIf="isDark"></i>
    </button>
  `,
})
export class DarkModeToggleComponent {
  isDark = false;

  constructor(
    private renderer: Renderer2,
    @Inject(DOCUMENT) private document: Document
  ) {
    const saved = localStorage.getItem('darkMode') === 'true';
    this.isDark = saved;
    this.applyTheme();
  }

  toggle() {
    this.isDark = !this.isDark;
    localStorage.setItem('darkMode', this.isDark.toString());
    this.applyTheme();
  }

  private applyTheme() {
    if (this.isDark) {
      this.renderer.addClass(this.document.body, 'dark');
    } else {
      this.renderer.removeClass(this.document.body, 'dark');
    }
  }
}