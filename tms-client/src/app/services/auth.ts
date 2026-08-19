import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface TmsUser {
  email: string;
  displayName: string;
  role: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private accessToken = signal<string | null>(null);
  private refreshToken = signal<string | null>(null);
  currentUser = signal<TmsUser | null>(null);

  getAccessToken(): string | null {
    return this.accessToken();
  }

  hasRole(role: string): boolean {
    const user = this.currentUser();
    return user?.role === role || user?.role === 'Admin';
  }

  async login(credentials: LoginRequest): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<AuthResponse>('/api/Auth/login', credentials)
    );

    this.accessToken.set(response.accessToken);
    this.refreshToken.set(response.refreshToken);

    const payload = this.decodePayload(response.accessToken);
    const roleClaim = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    const role = Array.isArray(roleClaim) ? roleClaim[0] : roleClaim;

    this.currentUser.set({
      email: payload['email'] ?? '',
      displayName: payload['FirstName'] ?? payload['email'] ?? 'User',
      role: role ?? 'Student'
    });
  }

  async refresh(): Promise<void> {
    const refreshToken = this.refreshToken();
    if (!refreshToken) throw new Error('No refresh token available.');

    const response = await firstValueFrom(
      this.http.post<AuthResponse>('/api/Auth/refresh', { refreshToken })
    );

    this.accessToken.set(response.accessToken);
    this.refreshToken.set(response.refreshToken);
  }

  logout(): void {
    this.accessToken.set(null);
    this.refreshToken.set(null);
    this.currentUser.set(null);
  }

  private decodePayload(token: string): Record<string, any> {
    const encodedPayload = token.split('.')[1];
    const base64 = encodedPayload.replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(atob(base64));
  }
}