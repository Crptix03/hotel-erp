import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { API_URL } from './api.config';

interface LoginResponse {
  token: string;
  name: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private session = signal<LoginResponse | null>(this.read());
  readonly user = this.session.asReadonly();
  readonly isLoggedIn = computed(() => this.session() !== null);

  get token(): string | null {
    return this.session()?.token ?? null;
  }

  login(email: string, password: string) {
    return this.http
      .post<LoginResponse>(`${API_URL}/api/auth/login`, { email, password })
      .pipe(
        tap((res) => {
          localStorage.setItem('session', JSON.stringify(res));
          this.session.set(res);
        })
      );
  }

  logout() {
    localStorage.removeItem('session');
    this.session.set(null);
    this.router.navigate(['/login']);
  }

  private read(): LoginResponse | null {
    try {
      const raw = localStorage.getItem('session');
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }
}