import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, map } from 'rxjs';
import { ApiService } from './api.service';

export interface LoginResponse {
  success?: boolean;
  token?: string;
  user?: any;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private tokenKey = 'auth_token';
  private currentUserSubject = new BehaviorSubject<any>(null);
  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private api: ApiService) {
    const token = localStorage.getItem(this.tokenKey);
    if (token) {
      this.currentUserSubject.next({ token });
    }
  }

  sendOtp(email: string): Observable<any> {
    return this.api.post('api/auth/send-otp', { email });
  }

  verifyOtp(email: string, otp: string): Observable<LoginResponse> {
    return this.api.post<LoginResponse>('api/auth/verify-otp', { email, otp }).pipe(
      map((res) => {
        if (res && (res as any).token) {
          localStorage.setItem(this.tokenKey, (res as any).token as string);
          this.currentUserSubject.next((res as any).user ?? { token: (res as any).token });
        }
        return res;
      })
    );
  }

  login(email: string, password: string): Observable<LoginResponse> {
    return this.api.post<LoginResponse>('api/auth/login', { email, password }).pipe(
      map((res) => {
        if (res && (res as any).token) {
          localStorage.setItem(this.tokenKey, (res as any).token as string);
          this.currentUserSubject.next((res as any).user ?? { token: (res as any).token });
        }
        return res;
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  getUserRole(): string | null {
    const token = this.getToken();
    if (!token) return null;
    try {
      const payloadBase64 = token.split('.')[1];
      const padded = payloadBase64.replace(/-/g, '+').replace(/_/g, '/')
        + '=='.slice(0, (4 - (payloadBase64.length % 4)) % 4);
      const payloadJson = atob(padded);
      const payload = JSON.parse(payloadJson);
      return payload['role'] || payload['roles'] || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || null;
    } catch {
      return null;
    }
  }
}
