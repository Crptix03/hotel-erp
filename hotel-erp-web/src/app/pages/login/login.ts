import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-slate-100 p-4">
      <form
        (ngSubmit)="submit()"
        class="w-full max-w-sm bg-white rounded-xl shadow p-8 space-y-4"
      >
        <div>
          <h1 class="text-2xl font-bold text-slate-800">Hotel Management</h1>
          <p class="text-sm text-slate-500">Sign in to continue</p>
        </div>

        @if (error()) {
          <div class="rounded bg-red-50 text-red-700 text-sm p-3">{{ error() }}</div>
        }

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Email</label>
          <input
            type="email"
            name="email"
            [(ngModel)]="email"
            required
            class="w-full rounded border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Password</label>
          <input
            type="password"
            name="password"
            [(ngModel)]="password"
            required
            class="w-full rounded border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <button
          type="submit"
          [disabled]="loading()"
          class="w-full rounded bg-blue-600 text-white font-medium py-2 hover:bg-blue-700 disabled:opacity-50"
        >
          {{ loading() ? 'Signing in...' : 'Sign in' }}
        </button>
      </form>
    </div>
  `,
})
export class Login {
  private auth = inject(AuthService);
  private router = inject(Router);

  email = '';
  password = '';
  error = signal('');
  loading = signal(false);

  submit() {
    this.error.set('');
    this.loading.set(true);
    this.auth.login(this.email, this.password).subscribe({
      next: () => this.router.navigate(['/']),
      error: () => {
        this.error.set('Invalid email or password');
        this.loading.set(false);
      },
    });
  }
}