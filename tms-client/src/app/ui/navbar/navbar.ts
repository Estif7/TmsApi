import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <nav class="navbar" *ngIf="auth.currentUser()">
      <div class="brand">
        <a routerLink="/student-dashboard"><strong>TMS</strong> Portal</a>
      </div>

      <div class="nav-links">
        <a 
          *ngIf="auth.hasRole('Student')" 
          routerLink="/student-dashboard" 
          routerLinkActive="active">
          Student Dashboard
        </a>
        <a 
          *ngIf="auth.hasRole('Instructor')" 
          routerLink="/dashboard" 
          routerLinkActive="active">
          Instructor Dashboard
        </a>
        <a 
          *ngIf="auth.hasRole('Admin')" 
          routerLink="/admin/courses" 
          routerLinkActive="active">
          Admin Panel
        </a>
      </div>

      <div class="user-profile">
        <div class="user-info">
          <span class="username">{{ auth.currentUser()?.displayName }}</span>
          <span class="role-badge">{{ auth.currentUser()?.role }}</span>
        </div>
        <button (click)="logout()" class="btn-logout">Logout</button>
      </div>
    </nav>
  `,
  styleUrl: './navbar.scss'
})
export class NavbarComponent {
  readonly auth = inject(AuthService);
  private router = inject(Router);

  logout(): void {
    this.auth.logout(); //
    this.router.navigate(['/login']);
  }
}