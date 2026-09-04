import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: '../../components/login/login.scss'
})
export class Register {
  private authService = inject(AuthService);
  private router = inject(Router);

  fullName = '';
  email = '';
  password = '';
  confirmPassword = '';
  role = 'Student'; // Default role selection

  loading = signal(false);
  errorMessage = signal<string | null>(null);
  showPassword = signal(false);

  togglePassword(): void {
    this.showPassword.update(val => !val);
  }

  async onSubmit() {
  if (this.password !== this.confirmPassword) {
    this.errorMessage.set('Passwords do not match.');
    return;
  }

  this.loading.set(true);
  this.errorMessage.set(null);

  try {
    await this.authService.register({
      fullName: this.fullName,
      email: this.email,
      password: this.password,
      role: this.role
    });

    await this.router.navigate(['/login'], {
      queryParams: { registered: 'true' }
    });
  } catch (err: any) {
    // Handle ASP.NET Core Identity array response
    if (err?.error?.errors && Array.isArray(err.error.errors)) {
      this.errorMessage.set(err.error.errors.join(' '));
    } else {
      this.errorMessage.set(
        err?.error?.detail || err?.error?.title || 'Registration failed. Check password requirements.'
      );
    }
  } finally {
    this.loading.set(false);
  }
}
}