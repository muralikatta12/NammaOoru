import { Injectable } from '@angular/core';
import { HttpEvent, HttpErrorResponse, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(private auth: AuthService, private router: Router) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((err: any) => {
        if (err instanceof HttpErrorResponse) {
          if (err.status === 401) {
            this.auth.logout?.();
            this.router.navigate(['/auth/login']);
          }

          const message =
            err.error?.message ?? err.error?.title ?? err.message ?? `HTTP ${err.status} - ${err.statusText}`;

          try {
            window.alert(message);
          } catch {
            console.error(message);
          }
        }

        return throwError(() => err);
      })
    );
  }
}
