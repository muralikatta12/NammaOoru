import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { User, AuthResponse } from '../models/user.model';
import { environment } from '../../../environments/environments';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/api/auth`;
  private tokenKey = 'nammaooru_token';
  private userKey = 'nammaooru_user';

  // Observable for components to subscribe to
  private userSubject = new BehaviorSubject<User | null>(null);
  public user$ = this.userSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadUserFromStorage();
  }

  // ================ PUBLIC METHODS ================

  register(data: any): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, data);
  }

sendOtp(email: string): Observable<any> {
  return this.http.post<any>(`${this.apiUrl}/send-otp`, { 
    email: email.trim().toLowerCase() 
  });
}

verifyOtp(email: string, otpCode: string): Observable<AuthResponse> {
  return this.http.post<AuthResponse>(`${this.apiUrl}/verify-otp`, { 
    email: email.trim().toLowerCase(), 
    otpCode 
  }).pipe(
    tap(res => {
      if (res.success && res.token && res.user) {
        localStorage.setItem(this.tokenKey, res.token!);
        localStorage.setItem(this.userKey, JSON.stringify(res.user!));
        this.userSubject.next(res.user!);
      }
    })
  );

}

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, { email, password })
      .pipe(tap(res => this.handleAuthSuccess(res)));
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    this.userSubject.next(null);
  }

  // ================ HELPERS ================

  private handleAuthSuccess(res: AuthResponse): void {
    if (res.success && res.token && res.user) {
      localStorage.setItem(this.tokenKey, res.token);
      localStorage.setItem(this.userKey, JSON.stringify(res.user));
      this.userSubject.next(res.user);
    }
  }

  private loadUserFromStorage(): void {
    const token = localStorage.getItem(this.tokenKey);
    const userJson = localStorage.getItem(this.userKey);
    if (token && userJson) {
      this.userSubject.next(JSON.parse(userJson));
    }
  }

  // ================ GETTERS ================

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getCurrentUser(): User | null {
    return this.userSubject.value;
  }

  getUserRole(): string | null {
    return this.userSubject.value?.role || null;
  }
}