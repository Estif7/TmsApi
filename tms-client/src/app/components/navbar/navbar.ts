import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs/operators';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'tms-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <nav class="tms-navbar" *ngIf="showNavbar()">
      <div class="nav-brand">
        <a routerLink="/dashboard">
          <span class="logo-accent">TMS</span> Portal
        </a>
      </div>

      <div class="nav-user" *ngIf="authService.currentUser() as user">
        <div class="user-details">
          <span class="user-name">{{ user.displayName }}</span>
        </div>
        <button type="button" class="btn-logout" (click)="onLogout()">
          Logout
        </button>
      </div>
    </nav>
  `,
  styles: [`
    .tms-navbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.85rem 2rem;
      background-color: #0f172a;
      color: #f8fafc;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }
    .nav-brand a {
      font-size: 1.25rem;
      font-weight: 700;
      color: #ffffff;
      text-decoration: none;
      .logo-accent { color: #38bdf8; }
    }
    .nav-user {
      display: flex;
      align-items: center;
      gap: 1.25rem;
    }
    .user-details {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
    }
    .user-name {
      font-size: 0.9rem;
      font-weight: 600;
      color: #f1f5f9;
    }
    .user-role {
      font-size: 0.7rem;
      padding: 0.1rem 0.4rem;
      border-radius: 4px;
      font-weight: 600;
      text-transform: uppercase;
      &.admin { background-color: #7c3aed; color: #ffffff; }
      &.instructor { background-color: #2563eb; color: #ffffff; }
      &.student { background-color: #059669; color: #ffffff; }
    }
    .btn-logout {
      background-color: #ef4444;
      color: white;
      border: none;
      padding: 0.4rem 0.85rem;
      border-radius: 4px;
      font-size: 0.85rem;
      font-weight: 500;
      cursor: pointer;
      &:hover { background-color: #dc2626; }
    }
  `]
})
export class NavbarComponent {
  readonly authService = inject(AuthService);
  private router = inject(Router);

  // Tracks current route URL dynamically via router events
  private currentUrl = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects)
    ),
    { initialValue: this.router.url }
  );

  showNavbar(): boolean {
    const user = this.authService.currentUser();
    const url = this.currentUrl();
    
    // Explicitly hide navbar if user is not logged in OR if on /login page
    return !!user && !url.startsWith('/login');
  }

  onLogout(): void {
    this.authService.logout();
  }
}