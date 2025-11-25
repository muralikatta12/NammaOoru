import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.html',
})
export class LoginComponent {
  loading = false;

  loginForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  onSubmit() {
    if (this.loginForm.invalid) return;

    this.loading = true;
    const { email, password } = this.loginForm.value;

    this.authService.login(email!, password!).subscribe({
      next: () => this.router.navigate(['/reports']),
      error: () => {
        alert('Login failed. Check email/password.');
        this.loading = false;
      }
    });
  }

  loginWithOtp() {
  const email = this.loginForm.get('email')?.value?.trim();
  if (!email) {
    alert('Please enter your email');
    return;
  }

  this.loading = true;

  // CALL BACKEND TO SEND OTP
  this.authService.sendOtp(email).subscribe({
    next: (res) => {
      if (res.success) {
        // Only go to OTP page AFTER backend confirms OTP sent
        this.router.navigate(['/auth/verify'], { state: { email } });
      } else {
        alert('Failed to send OTP: ' + res.message);
      }
      this.loading = false;
    },
    error: (err) => {
      alert('Network error. Is backend running?');
      this.loading = false;
    }
  });


  // Just go to OTP page — no API call here
  this.router.navigate(['/auth/verify'], { state: { email } });
}
  
}