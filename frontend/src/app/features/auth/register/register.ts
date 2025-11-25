import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register.html',
})
export class RegisterComponent {
  loading = false;


  registerForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.registerForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }


  onSubmit() {
  if (this.registerForm.invalid) return;

  this.loading = true;
  const data = this.registerForm.value;

  this.authService.register(data).subscribe({
    next: (res) => {
      if (res.success) {
        const email = this.registerForm.get('email')?.value;
        // Now send OTP
        this.authService.sendOtp(email).subscribe({
          next: () => {
            this.router.navigate(['/auth/verify'], { state: { email } });
          },
          error: () => alert('Registered, but failed to send OTP')
        });
      }
    },
    error: () => {
      alert('Registration failed');
      this.loading = false;
    }
  });
}
}