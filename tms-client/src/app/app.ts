import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './components/navbar/navbar';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <tms-navbar></tms-navbar>
    <main class="app-container">
      <router-outlet></router-outlet>
    </main>
  `,
  styles: [`
    .app-container {
      min-height: calc(100vh - 60px);
    }
  `]
})
export class App {}
