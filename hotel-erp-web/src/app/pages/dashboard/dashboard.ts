import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-dashboard',
  template: `
    <h1 class="text-2xl font-bold text-slate-800">Dashboard</h1>
    <p class="text-slate-600 mt-2">Welcome, {{ auth.user()?.name }}.</p>
  `,
})
export class Dashboard {
  auth = inject(AuthService);
}