import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs/operators';
import { AuthService } from '../../services/auth';
import { NavigationHistoryService } from '../../services/navigation-history';

@Component({
  selector: 'tms-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss'
})
export class NavbarComponent {
  readonly authService = inject(AuthService);
  private router = inject(Router);
  private navigationHistory = inject(NavigationHistoryService);

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

    return !!user && !url.startsWith('/login');
  }

  goBack(): void {
    this.navigationHistory.goBack();
  }

  onLogout(): void {
    this.authService.logout();
  }
}
