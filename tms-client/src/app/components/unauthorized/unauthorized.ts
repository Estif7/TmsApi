import { Component } from '@angular/core';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  template: '<main><h1>Access denied</h1><p>You do not have permission to view this page.</p></main>'
})
export class Unauthorized {}
