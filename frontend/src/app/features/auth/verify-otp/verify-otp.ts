// src/app/features/auth/verify-otp/verify-otp.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-verify-otp',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './verify-otp.html',
})
export class VerifyOtpComponent implements OnInit {
  email = '';
  loading = false;

  // THIS IS THE FIX — inject FormBuilder directly in form definition
  otpForm = inject(FormBuilder).group({
    otp: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
  });

  constructor(
    private auth: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    this.email = history.state.email || '';
    if (!this.email) {
      alert('Session expired. Please login again.');
      this.router.navigate(['/auth/login']);
    }
  }

  onSubmit() {
    if (this.otpForm.invalid) return alert('Enter valid 6-digit OTP');

    this.loading = true;

    this.auth.verifyOtp(this.email, this.otpForm.value.otp!).subscribe({
      next: (res) => {
        if (!res.success) {
          alert(res.message || 'Invalid OTP');
          this.loading = false;
          return;
        }

        // MARK USER AS VERIFIED — badge turns green instantly!
        const user = this.auth.getCurrentUser();
        if (user) {
          user.isVerified = true;
          localStorage.setItem('user', JSON.stringify(user));
        }

        this.router.navigate(['/']);
      },
      error: () => {
        alert('Verification failed');
        this.loading = false;
      }
    });
  }

  resendOtp() {
    this.loading = true;
    this.auth.sendOtp(this.email).subscribe({
      next: () => alert('New OTP sent!'),
      error: () => alert('Failed to resend'),
      complete: () => this.loading = false
    });
  }
}