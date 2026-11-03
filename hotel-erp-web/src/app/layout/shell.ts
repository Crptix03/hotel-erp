import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="min-h-screen flex bg-slate-100">
      <aside class="w-64 bg-slate-900 text-slate-200 flex flex-col">
        <div class="px-6 py-5 border-b border-slate-700">
          <div class="text-lg font-bold text-white">Gezana Hospitality</div>
          <div class="text-xs text-slate-400">Hotel Management</div>
        </div>

        <nav class="flex-1 px-3 py-4 space-y-1 text-sm">
          <a
            routerLink="/dashboard"
            routerLinkActive="bg-slate-700 text-white"
            class="block rounded px-3 py-2 hover:bg-slate-800"
          >
            Dashboard
          </a>
          <span class="block rounded px-3 py-2 text-slate-500">Property &amp; Rooms (soon)</span>
          <span class="block rounded px-3 py-2 text-slate-500">Rates &amp; Packages (soon)</span>
          <span class="block rounded px-3 py-2 text-slate-500">Reservations (soon)</span>
          <span class="block rounded px-3 py-2 text-slate-500">Folio &amp; Billing (soon)</span>
          <span class="block rounded px-3 py-2 text-slate-500">Guest Services (soon)</span>
          <span class="block rounded px-3 py-2 text-slate-500">Reports (soon)</span>
        </nav>

        <div class="px-6 py-4 border-t border-slate-700 text-sm">
          <div class="text-white">{{ auth.user()?.name }}</div>
          <div class="text-xs text-slate-400 mb-2">{{ auth.user()?.role }}</div>
          <button (click)="auth.logout()" class="text-blue-300 hover:text-white">
            Sign out
          </button>
        </div>
      </aside>

      <main class="flex-1 p-8">
        <router-outlet />
      </main>
    </div>
  `,
})
export class Shell {
  auth = inject(AuthService);
}