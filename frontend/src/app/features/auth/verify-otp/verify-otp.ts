import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-verify-otp',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './verify-otp.html',
  styleUrls: ['./verify-otp.css'] // optional
})
export class VerifyOtpComponent implements OnInit {
  email = '';
  loading = false;

  // Fix: Declare fb first, then use it
  private fb = new FormBuilder();
  otpForm = this.fb.group({
    otp: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
  });

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    // Get email from navigation state (passed from login/register)
    const navigation = this.router.getCurrentNavigation();
    this.email = (navigation?.extras?.state as any)?.email || 'your email';
  }

  
  verify() {
  if (this.otpForm.invalid || this.loading) return;

  this.loading = true;
  const otpCode = this.otpForm.get('otp')!.value!.trim();
  const email = this.email.trim().toLowerCase();  // ← CRITICAL: normalize

  this.authService.verifyOtp(email, otpCode).subscribe({
    next: (res) => {
      if (res.success) {
        this.router.navigate(['/dashboard']);
      } else {
        alert(res.message || 'Invalid OTP');
        this.loading = false;
      }
    },
    error: (err) => {
      const msg = err.error?.message || err.message || 'Server error';
      alert('OTP Verification Failed: ' + msg);
      this.loading = false;
    }
  });

}
}