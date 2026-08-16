import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="login-container">
      <h2>TMS Admin Login</h2>
      <form (ngSubmit)="onSubmit()">
        <div class="form-group">
          <label for="email">Email</label>
          <input
            id="email"
            type="email"
            [(ngModel)]="email"
            name="email"
            required
            placeholder="admin@tms.com"
          />
        </div>

        <div class="form-group">
          <label for="password">Password</label>
          <input
            id="password"
            type="password"
            [(ngModel)]="password"
            name="password"
            required
            placeholder="Admin123!"
          />
        </div>

        @if (errorMessage()) {
          <div class="error-banner">{{ errorMessage() }}</div>
        }

        <button type="submit" [disabled]="loading()">
          {{ loading() ? 'Logging in...' : 'Login' }}
        </button>
      </form>
    </div>
  `,
  styles: [`
    .login-container {
      max-width: 400px;
      margin: 80px auto;
      padding: 30px;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0,0,0,0.05);
      font-family: sans-serif;
    }
    .form-group {
      margin-bottom: 20px;
    }
    .form-group label {
      display: block;
      margin-bottom: 6px;
      font-weight: 600;
    }
    .form-group input {
      width: 100%;
      padding: 10px;
      box-sizing: border-box;
      border: 1px solid #ccc;
      border-radius: 4px;
    }
    button {
      width: 100%;
      padding: 12px;
      background-color: #0066cc;
      color: white;
      border: none;
      border-radius: 4px;
      font-weight: bold;
      cursor: pointer;
    }
    button:disabled {
      background-color: #99ccff;
    }
    .error-banner {
      color: #d9534f;
      background-color: #fdf7f7;
      border: 1px solid #d9534f;
      padding: 10px;
      border-radius: 4px;
      margin-bottom: 20px;
    }
  `]
})
export class Login {
  private authService = inject(AuthService);
  private router = inject(Router);

  email = 'admin@tms.com';
  password = 'Admin123!';
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  async onSubmit() {
    this.loading.set(true);
    this.errorMessage.set(null);

    try {
      await this.authService.login({ email: this.email, password: this.password });
      await this.router.navigate(['/']); // Redirect to home/dashboard on successful login
    } catch (err: any) {
      this.errorMessage.set(err?.error?.detail || 'Invalid login credentials.');
    } finally {
      this.loading.set(false);
    }
  }
}