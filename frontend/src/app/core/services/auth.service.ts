// src/app/core/services/auth.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environments';

export interface User {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  isVerified?: boolean;
  emailVerified?: boolean;
  phoneVerified?: boolean;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadUserFromStorage(); // ← Load on app start
  }

  private loadUserFromStorage() {
    const token = localStorage.getItem('token');
    const userJson = localStorage.getItem('currentUser');
    if (token && userJson) {
      const user = JSON.parse(userJson) as User;
      this.currentUserSubject.next(user);
    }
  }

  sendOtp(email: string): Observable<any> {
    return this.http.post(`${environment.apiUrl}/auth/send-otp`, { email });
  }

  verifyOtp(email: string, otpCode: string): Observable<any> {
    return this.http.post<any>(`${environment.apiUrl}/auth/verify-otp`, { email, otpCode })
      .pipe(
        tap(res => {
          if (res.success && res.token && res.user) {
            this.setSession(res.token, res.user);
          }
        })
      );
  }

  // src/app/core/services/auth.service.ts
login(otp: string) {
  return this.http.post<any>(`${environment.apiUrl}/api/Auth/Login`, { otp }).pipe(
    tap(res => {
      if (res && res.token) {
        localStorage.setItem('token', res.token);  // ← MUST BE 'token'
        // Optional: decode and save user
        const payload = JSON.parse(atob(res.token.split('.')[1]));
        localStorage.setItem('user', JSON.stringify(payload));
      }
    })
  );
}

  register(data: any): Observable<any> {
    return this.http.post<any>(`${environment.apiUrl}/auth/register`, data);
  }

  private setSession(token: string, user: User) {
    localStorage.setItem('token', token);
    localStorage.setItem('currentUser', JSON.stringify(user)); // ← SAVE FULL USER
    this.currentUserSubject.next(user); // ← Update everyone
  }

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('currentUser');
    this.currentUserSubject.next(null);
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }
}