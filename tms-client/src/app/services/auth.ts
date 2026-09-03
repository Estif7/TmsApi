import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';

export interface TmsUser {
  id: string | number;
  displayName: string;
  email: string;
  role?: string[];
  studentId?: number;
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
  private router = inject(Router);

  // Synchronously initialize signals from localStorage
  private accessToken = signal<string | null>(localStorage.getItem('access_token'));
  private refreshTokenSignal = signal<string | null>(localStorage.getItem('refresh_token'));

  currentUser = signal<TmsUser | null>(this.getInitialUser());

  getAccessToken(): string | null {
    return this.accessToken();
  }

  // FIX 1: Safely handle role comparisons when user.role is an array of strings
  hasRole(role: string): boolean {
    const user = this.currentUser();
    if (!user || !user.role) return false;

    return user.role.includes(role) || user.role.includes('Admin');
  }

  async login(credentials: LoginRequest): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<AuthResponse>('/api/Auth/login', credentials)
    );

    this.saveTokens(response.accessToken, response.refreshToken);

    // Update user signal immediately after successful login
    const user = this.buildUserFromToken(response.accessToken);
    this.currentUser.set(user);
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

    this.router.navigate(['/login'], { replaceUrl: true });
  }

  private saveTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem('access_token', accessToken);
    localStorage.setItem('refresh_token', refreshToken);
    this.accessToken.set(accessToken);
    this.refreshTokenSignal.set(refreshToken);
  }

  private getInitialUser(): TmsUser | null {
    const token = localStorage.getItem('access_token');
    if (!token) {
      return null;
    }
    try {
      return this.buildUserFromToken(token);
    } catch {
      return null;
    }
  }

  private buildUserFromToken(token: string): TmsUser {
  const payload = this.decodePayload(token);

  // Read standard Microsoft/JWT claims
  const roleClaim =
    payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
    payload['role'];

  const rawSub =
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ??
    payload['sub'] ??
    payload['id'];

  // Explicit student ID claim or fallback
  const rawStudentId = payload['studentId'] ?? payload['StudentId'] ?? rawSub;

  const parsedStudentId = parseInt(rawStudentId, 10);
  const validStudentId = !isNaN(parsedStudentId) && parsedStudentId > 0 ? parsedStudentId : 1;

  const roles: string[] = Array.isArray(roleClaim)
    ? roleClaim
    : roleClaim
    ? [roleClaim]
    : ['Student'];

  return {
    id: rawSub ?? 1,
    studentId: validStudentId,
    email:
      payload['email'] ??
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ??
      '',
    displayName:
      payload['FirstName'] ?? payload['displayName'] ?? payload['email'] ?? 'Student',
    role: roles,
  };
}

  private decodePayload(token: string): Record<string, any> {
    const encodedPayload = token.split('.')[1];
    const base64 = encodedPayload.replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(atob(base64));
  }
}