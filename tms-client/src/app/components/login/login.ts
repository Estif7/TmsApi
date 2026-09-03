import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  private authService = inject(AuthService);
  private router = inject(Router);

  email = 'admin@test.com';
  password = 'Admintest@123';
  rememberMe = false;

  loading = signal(false);
  errorMessage = signal<string | null>(null);
  showPassword = signal(false);

  togglePassword(): void {
    this.showPassword.update(value => !value);
  }

  forgotPassword(): void {
    console.log('Forgot password clicked');
  }

  async onSubmit() {
    this.loading.set(true);
    this.errorMessage.set(null);

    try {
      await this.authService.login({
        email: this.email,
        password: this.password
      });

      const roles = this.authService.currentUser()?.role ?? [];

      // Determine redirect path by checking if the role array contains specific roles
      const destination = roles.includes('Admin')
        ? '/admin/courses'
        : roles.includes('Instructor')
          ? '/dashboard'
          : '/student-dashboard';

      await this.router.navigate([destination], { replaceUrl: true });

    } catch (err: any) {
      this.errorMessage.set(
        err?.error?.detail ||
        'Invalid login credentials.'
      );
    } finally {
      this.loading.set(false);
    }
  }
}