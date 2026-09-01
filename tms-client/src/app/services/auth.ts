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

  // Synchronously initialize signals from localStorage
  private accessToken = signal<string | null>(localStorage.getItem('access_token'));
  private refreshTokenSignal = signal<string | null>(localStorage.getItem('refresh_token'));
  
  currentUser = signal<TmsUser | null>(this.getInitialUser());

  constructor() {
    // Optionally trigger token refresh in background on startup if token exists
  }

  getAccessToken(): string | null {
    return this.accessToken();
  }

  hasRole(role: string): boolean {
    const user = this.currentUser();
    if (!user) return false;
    return user.role === role || user.role === 'Admin';
  }

  async login(credentials: LoginRequest): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<AuthResponse>('/api/Auth/login', credentials)
    );

    this.saveTokens(response.accessToken, response.refreshToken);
    this.currentUser.set(this.buildUserFromToken(response.accessToken));
  }

  async refresh(): Promise<void> {
    const refreshToken = this.refreshTokenSignal();
    if (!refreshToken) throw new Error('No refresh token available.');

    const response = await firstValueFrom(
      this.http.post<AuthResponse>('/api/Auth/refresh', { refreshToken })
    );

    this.saveTokens(response.accessToken, response.refreshToken);
    this.currentUser.set(this.buildUserFromToken(response.accessToken));
  }

  logout(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    this.accessToken.set(null);
    this.refreshTokenSignal.set(null);
    this.currentUser.set(null);
  }

  private saveTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem('access_token', accessToken);
    localStorage.setItem('refresh_token', refreshToken);
    this.accessToken.set(accessToken);
    this.refreshTokenSignal.set(refreshToken);
  }

  private getInitialUser(): TmsUser | null {
    const token = localStorage.getItem('access_token');
    if (!token) return null;
    try {
      return this.buildUserFromToken(token);
    } catch {
      return null;
    }
  }

  private buildUserFromToken(token: string): TmsUser {
    const payload = this.decodePayload(token);
    const roleClaim = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    const role = Array.isArray(roleClaim) ? roleClaim[0] : roleClaim;

    return {
      email: payload['email'] ?? '',
      displayName: payload['FirstName'] ?? payload['email'] ?? 'User',
      role: role ?? 'Student'
    };
  }

  private decodePayload(token: string): Record<string, any> {
    const encodedPayload = token.split('.')[1];
    const base64 = encodedPayload.replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(atob(base64));
  }
}