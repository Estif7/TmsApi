import { Injectable, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class NavigationHistoryService {
  private router = inject(Router);

  private history: string[] = [];

  constructor() {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd =>
          event instanceof NavigationEnd
        )
      )
      .subscribe(event => {
        const url = event.urlAfterRedirects;

        // Don't store duplicate consecutive routes
        if (this.history[this.history.length - 1] !== url) {
          this.history.push(url);
        }

        // Keep only the last few routes
        if (this.history.length > 20) {
          this.history.shift();
        }
      });
  }

  goBack(): void {
    // Need at least 2 routes:
    // [previous page, current page]
    if (this.history.length < 2) {
      return;
    }

    // Remove current page
    this.history.pop();

    // Get previous page
    const previousUrl = this.history[this.history.length - 1];

    // Navigate using Angular Router, NOT browser history
    this.router.navigateByUrl(previousUrl);
  }
}
